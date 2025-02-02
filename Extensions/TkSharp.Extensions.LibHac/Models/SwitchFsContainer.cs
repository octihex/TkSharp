using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;

namespace TkSharp.Extensions.LibHac.Models;

internal sealed class SwitchFsContainer : List<(string Label, SwitchFs Fs)>, IDisposable
{
    private readonly List<IDisposable> _cleanup = [];
    
    [Pure]
    public IEnumerable<SwitchFs> AsFsList() => this.Select(s => s.Fs);

    public void CleanupLater(IDisposable fs)
    {
        _cleanup.Add(fs);
    }
    
    public void Dispose()
    {
        foreach ((_, SwitchFs fs) in this) {
            fs.Dispose();
        }
        
        foreach (IDisposable disposable in _cleanup) {
            disposable.Dispose();
        }
    }
}