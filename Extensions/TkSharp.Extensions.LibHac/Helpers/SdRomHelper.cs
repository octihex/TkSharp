using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using TkSharp.Extensions.LibHac.Common;

namespace TkSharp.Extensions.LibHac.Helpers;

public class SdRomHelper : ILibHacRomHelper
{
    private UniqueRef<IAttributeFileSystem> _localFsRef;

    public SwitchFs Initialize(string sdCardPath, KeySet keys)
    {
        FatFileSystem.Create(out FatFileSystem? localFs, sdCardPath).ThrowIfFailure();
        _localFsRef = new UniqueRef<IAttributeFileSystem>(localFs);

        return SwitchFs.OpenSdCard(keys, ref _localFsRef);
    }

    public void Dispose()
    {
        _localFsRef.Destroy();
        GC.SuppressFinalize(this);
    }
}