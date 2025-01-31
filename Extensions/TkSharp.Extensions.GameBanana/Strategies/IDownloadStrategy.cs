using TkSharp.Extensions.GameBanana.Models;

namespace TkSharp.Extensions.GameBanana.Strategies;

public interface IDownloadStrategy
{
    ValueTask<byte[]> GetBytesAndReportProgress(Uri url, DownloadReporter? reporter, CancellationToken ct = default);
}