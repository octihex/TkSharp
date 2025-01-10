using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TkSharp.Extensions.GameBanana.Strategies;

public class ParallelDownloadStrategy : IDownloadStrategy
{
    private const int SEGMENTS = 7;
    private const int BUFFER_SIZE = 65536; // 64KB buffer
    private const int TIMEOUT_MS = 5000;

    public async Task<byte[]> GetBytesAndReportProgress(
        string url,
        HttpClient client,
        Func<IProgress<double>?> onStarted,
        Action onCompleted,
        Action<double> onProgress,
        CancellationToken ct = default)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is not { } contentLength)
        {
            onStarted();
            byte[] staticResult = await response.Content.ReadAsByteArrayAsync(ct);
            onCompleted();
            return staticResult;
        }

        byte[] result = new byte[contentLength];
        var downloadQueue = new ConcurrentQueue<(int segmentIndex, long start, long end)>();

        // Calculate segment sizes ensuring no bytes are missed
        long segmentSize = (long)Math.Ceiling((double)contentLength / SEGMENTS);
        long currentPosition = 0;

        for (int i = 0; i < SEGMENTS && currentPosition < contentLength; i++)
        {
            long end = Math.Min(currentPosition + segmentSize - 1, contentLength - 1);
            downloadQueue.Enqueue((i, currentPosition, end));
            currentPosition += segmentSize;
        }

        long totalBytesDownloaded = 0;
        object lockObject = new object();
        var speedTimer = Stopwatch.StartNew();
        long bytesDownloadedInInterval = 0;

        IProgress<double>? progress = onStarted();

        var downloadTasks = new Task[SEGMENTS];
        for (int i = 0; i < SEGMENTS; i++)
        {
            downloadTasks[i] = Task.Run(async () =>
            {
                while (downloadQueue.TryDequeue(out var segment))
                {
                    int segmentIndex = segment.segmentIndex;
                    long start = segment.start;
                    long end = segment.end;
                    long expectedBytes = end - start + 1;

                    int attempt = 0;
                    const int maxRetry = 5;
                    bool success = false;

                    while (attempt < maxRetry && !success)
                    {
                        try
                        {
                            using var request = new HttpRequestMessage(HttpMethod.Get, url);
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

                            using var segmentResponse = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                            if (!segmentResponse.IsSuccessStatusCode)
                            {
                                attempt++;
                                if (attempt < maxRetry)
                                {
                                    await Task.Delay(100 * attempt, ct);
                                }
                                continue;
                            }

                            await using var responseStream = await segmentResponse.Content.ReadAsStreamAsync(ct);
                            byte[] buffer = new byte[Math.Min(BUFFER_SIZE, expectedBytes)];
                            long segmentBytesRead = 0;

                            while (segmentBytesRead < expectedBytes)
                            {
                                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                timeoutCts.CancelAfter(TIMEOUT_MS);

                                int bytesRead = await responseStream.ReadAsync(
                                    buffer.AsMemory(0, (int)Math.Min(buffer.Length, expectedBytes - segmentBytesRead)),
                                    timeoutCts.Token);

                                if (bytesRead == 0) break;

                                lock (lockObject)
                                {
                                    Array.Copy(buffer, 0, result, start + segmentBytesRead, bytesRead);
                                    totalBytesDownloaded += bytesRead;
                                    bytesDownloadedInInterval += bytesRead;
                                    segmentBytesRead += bytesRead;

                                    onProgress((double)totalBytesDownloaded / contentLength);
                                    progress?.Report((double)totalBytesDownloaded / contentLength);
                                }
                            }

                            success = segmentBytesRead == expectedBytes;
                            if (!success)
                            {
                                attempt++;
                                if (attempt < maxRetry)
                                {
                                    await Task.Delay(100 * attempt, ct);
                                }
                            }
                        }
                        catch (Exception) when (!ct.IsCancellationRequested)
                        {
                            attempt++;
                            if (attempt < maxRetry)
                            {
                                await Task.Delay(100 * attempt, ct);
                            }
                        }
                    }
                }
            }, ct);
        }

        try
        {
            await Task.WhenAll(downloadTasks);
        }
        finally
        {
            onCompleted();
        }

        return result;
    }
}
