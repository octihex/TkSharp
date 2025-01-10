namespace TkSharp.Extensions.GameBanana.Strategies;

public class SimpleDownloadStrategy : IDownloadStrategy
{
    public async Task<byte[]> GetBytesAndReportProgress(
        string url,
        HttpClient client,
        Func<IProgress<double>?> onStarted,
        Action onCompleted,
        Action<double> onProgress,
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var progressReporter = onStarted();

        if (response.Content.Headers.ContentLength is not { } contentLength) {
            // If the length is not known ahead
            // of time, return the whole buffer
            onStarted();
            byte[] staticResult = await response.Content.ReadAsByteArrayAsync(ct);
            onCompleted();
            return staticResult;
        }

        const int frameBufferSize = 0x2000;

        byte[] result = new byte[contentLength];
        Memory<byte> buffer = result;
        int bytesRead = 0;

        await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
        while (bytesRead < contentLength) {
            int nextOffset = (bytesRead + frameBufferSize) >= result.Length
                ? result.Length
                : bytesRead + frameBufferSize;
            int read = await stream.ReadAsync(buffer[bytesRead..nextOffset], ct);
            bytesRead += read;
            
            var currentProgress = (double)bytesRead / contentLength;
            onProgress(currentProgress);
            progressReporter?.Report(currentProgress);
        }

        onCompleted();
        return result;
    }
} 