using System.Diagnostics;
using TkSharp.Extensions.GameBanana.Models;

namespace TkSharp.Extensions.GameBanana.Strategies;

public class SimpleDownloadStrategy(HttpClient client) : IDownloadStrategy
{
    private const int FRAME_BUFFER_SIZE = 0x2000;
    
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
        Memory<byte> buffer = result;
        int bytesRead = 0;

        int bytesReadAtLastFrame = 0;
        long startTime = Stopwatch.GetTimestamp();
        await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
        
        while (bytesRead < contentLength) {
            int nextOffset = Math.Min(bytesRead + FRAME_BUFFER_SIZE, result.Length);
            int read = await stream.ReadAsync(buffer[bytesRead..nextOffset], ct);
            if (read == 0) break;

            bytesRead += read;

            if (reporter is not null && Stopwatch.GetElapsedTime(startTime).TotalSeconds >= 1) {
                double bytesPerSecond = bytesRead - bytesReadAtLastFrame; 
                reporter.ReportSpeed(bytesPerSecond / (1024.0 * 1024.0));
                bytesReadAtLastFrame = bytesRead;
            }
            
            reporter?.ReportProgress((double)bytesRead / contentLength);
        }

        return result;
    }
} 