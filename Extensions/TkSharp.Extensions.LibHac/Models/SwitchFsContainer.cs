using LibHac.Tools.Fs;

namespace TkSharp.Extensions.LibHac.Models;

internal sealed class SwitchFsContainer : List<SwitchFs>, IDisposable
{
    public void Dispose()
    {
        foreach (SwitchFs fs in this) {
            fs.Dispose();
        }
    }
}