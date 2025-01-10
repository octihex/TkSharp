using System.Net;

namespace TkSharp.Extensions.GameBanana.Strategies;

public interface IDownloadStrategy
{
    Task<byte[]> GetBytesAndReportProgress(
        string url, 
        HttpClient client,
        Func<IProgress<double>?> onStarted,
        Action onCompleted,
        Action<double> onProgress,
        CancellationToken ct = default);
} 