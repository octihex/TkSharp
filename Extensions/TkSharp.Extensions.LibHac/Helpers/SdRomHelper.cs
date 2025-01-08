using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using TkSharp.Extensions.LibHac.Common;

namespace TkSharp.Extensions.LibHac.Helpers;

public class SdRomHelper : ILibHacRomHelper
{
    private UniqueRef<IAttributeFileSystem> _localFsRef;

    public SwitchFs Initialize(string sdCardPath, KeySet keys)
    {
        LocalFileSystem.Create(out LocalFileSystem? localFs, sdCardPath).ThrowIfFailure();
        var archiveFs = new ArchiveFileSystem(localFs);
        _localFsRef = new UniqueRef<IAttributeFileSystem>(archiveFs);

        var concatFs = new ConcatenationFileSystem(ref _localFsRef);

        using var contentDirPath = new global::LibHac.Fs.Path();
        PathFunctions.SetUpFixedPath(ref contentDirPath.Ref(), "/Nintendo/Contents"u8).ThrowIfFailure();

        var contentDirFs = new SubdirectoryFileSystem(concatFs);
        contentDirFs.Initialize(in contentDirPath).ThrowIfFailure();

        var encFs = new AesXtsFileSystem(contentDirFs, keys.SdCardEncryptionKeys[1].DataRo.ToArray(), 0x4000);
        return new SwitchFs(keys, encFs, null);
    }

    public void Dispose()
    {
        _localFsRef.Destroy();
        GC.SuppressFinalize(this);
    }
}