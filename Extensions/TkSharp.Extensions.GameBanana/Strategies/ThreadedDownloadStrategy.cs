using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using TkSharp.Extensions.GameBanana.Models;

namespace TkSharp.Extensions.GameBanana.Strategies;

public class ThreadedDownloadStrategy(HttpClient client) : IDownloadStrategy
{
    private const int BUFFER_SIZE = 0x10000; // 64KB buffer
    private const int TIMEOUT_MS = 4000;
    private const long MB = 0x100000;

    public async ValueTask<byte[]> GetBytesAndReportProgress(Uri url, DownloadReporter? reporter, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is not { } contentLength) {
            // If the length is not known ahead
            // of time, return the whole buffer
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        byte[] result = new byte[contentLength];
        var downloadQueue = new ConcurrentQueue<(long start, long end)>();

        int segments = GetSegmentCount(contentLength);
        long segmentSize = (long)Math.Ceiling((double)contentLength / segments);
        long currentPosition = 0;

        for (int i = 0; i < segments && currentPosition < contentLength; i++) {
            long end = Math.Min(currentPosition + segmentSize - 1, contentLength - 1);
            downloadQueue.Enqueue((currentPosition, end));
            currentPosition += segmentSize;
        }

        long totalBytesDownloaded = 0;
        object @lock = new();
        var speedTimer = Stopwatch.StartNew();
        long bytesDownloadedInInterval = 0;

        if (reporter is not null) {
            await using var speedReportTimer = new Timer(_ => {
                double elapsedSeconds = speedTimer.Elapsed.TotalSeconds;
                if (elapsedSeconds > 0) {
                    long bytesInInterval = Interlocked.Exchange(ref bytesDownloadedInInterval, 0);
                    double bytesPerSecond = (bytesInInterval / elapsedSeconds);
                    double megabytesPerSecond = bytesPerSecond / MB;
                    reporter.ReportSpeed(megabytesPerSecond);
                    speedTimer.Restart();
                }
            }, null, 0, 1000);
        }

        var downloadTasks = new Task[segments];
        for (int i = 0; i < segments; i++) {
            downloadTasks[i] = Task.Run<Task>(async () => {
                while (downloadQueue.TryDequeue(out (long start, long end) segment)) {
                    long start = segment.start;
                    long end = segment.end;
                    long expectedBytes = end - start + 1;

                    int attempt = 0;
                    const int maxRetry = 5;
                    bool success = false;
                    int consecutiveTimeouts = 0;

                    while (attempt < maxRetry && !success) {
                        try {
                            long segmentBytesRead = 0;
                            using var request = new HttpRequestMessage(HttpMethod.Get, url);
                            long resumePosition = start + segmentBytesRead;
                            request.Headers.Range = new RangeHeaderValue(resumePosition, end);

                            using HttpResponseMessage segmentResponse = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                            if (!segmentResponse.IsSuccessStatusCode) {
                                attempt++;
                                if (attempt < maxRetry) {
                                    await Task.Delay(100 * attempt, ct);
                                }

                                continue;
                            }

                            await using Stream responseStream = await segmentResponse.Content.ReadAsStreamAsync(ct);
                            byte[] buffer = new byte[Math.Min(BUFFER_SIZE, expectedBytes - segmentBytesRead)];

                            while (segmentBytesRead < expectedBytes) {
                                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                timeoutCts.CancelAfter(TIMEOUT_MS);

                                int bytesRead = await responseStream.ReadAsync(
                                    buffer.AsMemory(0, (int)Math.Min(buffer.Length, expectedBytes - segmentBytesRead)),
                                    timeoutCts.Token);

                                if (bytesRead == 0) {
                                    consecutiveTimeouts++;
                                    if (consecutiveTimeouts >= maxRetry) {
                                        throw new TimeoutException($"No data received for {consecutiveTimeouts * 4} seconds");
                                    }

                                    continue;
                                }

                                consecutiveTimeouts = 0;

                                lock (@lock) {
                                    Array.Copy(buffer, 0, result, start + segmentBytesRead, bytesRead);
                                    totalBytesDownloaded += bytesRead;
                                    Interlocked.Add(ref bytesDownloadedInInterval, bytesRead);
                                    segmentBytesRead += bytesRead;

                                    double currentProgress = (double)totalBytesDownloaded / contentLength;
                                    reporter?.ReportProgress(currentProgress);
                                }
                            }

                            success = segmentBytesRead == expectedBytes;
                        }
                        catch (Exception) {
                            attempt++;
                            if (attempt < maxRetry) {
                                await Task.Delay(100 * attempt, ct);
                            }
                        }
                    }

                    if (!success) {
                        throw new TimeoutException($"Failed to download segment after {maxRetry} attempts");
                    }
                }

                return Task.CompletedTask;
            }, ct).Unwrap();
        }

        await Task.WhenAll(downloadTasks);
        
        return result;
    }

    private static int GetSegmentCount(long fileSize)
    {
        return fileSize switch {
            < 5 * MB => 1,
            < 10 * MB => 2,
            < 20 * MB => 4,
            < 30 * MB => 6,
            < 40 * MB => 8,
            _ => 10
        };
    }
}