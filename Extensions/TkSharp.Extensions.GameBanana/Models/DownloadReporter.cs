using System.Runtime.CompilerServices;

namespace TkSharp.Extensions.GameBanana.Models;

public sealed class DownloadReporter
{
    public required IProgress<double> ProgressReporter { get; init; }
    
    public required IProgress<double> SpeedReporter { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportProgress(double progress)
    {
        ProgressReporter.Report(progress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReportSpeed(double speed)
    {
        SpeedReporter.Report(speed);
    }
}