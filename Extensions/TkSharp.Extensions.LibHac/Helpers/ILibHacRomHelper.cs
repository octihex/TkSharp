using LibHac.Common.Keys;
using LibHac.Tools.Fs;

namespace TkSharp.Extensions.LibHac.Helpers
{
    public interface ILibHacRomHelper : IDisposable
    {
        SwitchFs Initialize(string path, KeySet keys);
    }
} 