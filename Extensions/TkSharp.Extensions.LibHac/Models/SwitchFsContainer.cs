using System.Diagnostics.Contracts;
using LibHac.Tools.Fs;

namespace TkSharp.Extensions.LibHac.Models;

internal sealed class SwitchFsContainer : List<(string Label, SwitchFs Fs)>, IDisposable
{
    [Pure]
    public IEnumerable<SwitchFs> AsFsList() => this.Select(s => s.Fs);
    
    public void Dispose()
    {
        foreach ((_, SwitchFs fs) in this) {
            fs.Dispose();
        }
    }
}