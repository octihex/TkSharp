using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using TkSharp.Core;
using TkSharp.Extensions.LibHac.Helpers;

namespace TkSharp.Extensions.LibHac;

public class LibHacRomProvider : IDisposable
{
    public const ulong EX_KING_APP_ID = 0x0100F2C0115B6000;

    private SwitchFs? _baseFs;
    private SwitchFs? _updateFs;
    private IFileSystem? _fileSystem;
    private ILibHacRomHelper? _helper;

    public TkRom CreateRom(TkChecksums checksums, KeySet keys, LibHacRomSourceType baseSourceType, string basePath, LibHacRomSourceType updateSourceType, string updatePath)
    {
        if (baseSourceType is LibHacRomSourceType.SdCard && updateSourceType is LibHacRomSourceType.SdCard && basePath == updatePath) {
            _helper = new SdRomHelper();
            _baseFs = _helper.Initialize(basePath, keys);
            _fileSystem = InitializeLayeredFs(_baseFs, _baseFs);
        }
        else {
            _baseFs = CreateSwitchFs(baseSourceType, basePath, keys);
            _updateFs = CreateSwitchFs(updateSourceType, updatePath, keys);
            _fileSystem = InitializeLayeredFs(_baseFs, _updateFs);
        }

        return new TkRom(checksums, _fileSystem);
    }

    private SwitchFs CreateSwitchFs(LibHacRomSourceType sourceType, string path, KeySet keys)
    {
        _helper = sourceType switch {
            LibHacRomSourceType.File => new FileRomHelper(),
            LibHacRomSourceType.SdCard => new SdRomHelper(),
            LibHacRomSourceType.SplitFiles => new SplitRomHelper(),
            _ => throw new ArgumentException($"Invalid source: {sourceType}")
        };

        return _helper.Initialize(path, keys);
    }

    private static IFileSystem InitializeLayeredFs(SwitchFs baseFs, SwitchFs updateFs)
    {
        return baseFs.Applications[EX_KING_APP_ID].Main.MainNca.Nca
            .OpenFileSystemWithPatch(updateFs.Applications[EX_KING_APP_ID].Patch.MainNca.Nca,
                NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
    }

    public void Dispose()
    {
        _fileSystem?.Dispose();
        _baseFs?.Dispose();
        _updateFs?.Dispose();
        _helper?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}

public enum LibHacRomSourceType
{
    File, // XCI or NSP file
    SdCard, // From SD card
    SplitFiles // Split files in a directory
}
