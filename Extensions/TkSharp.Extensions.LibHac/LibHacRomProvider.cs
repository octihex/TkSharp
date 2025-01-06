using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using TkSharp.Core;
using TkSharp.Extensions.LibHac.Extensions;
using TkSharp.Extensions.LibHac.Helpers;
using Path = System.IO.Path;

namespace TkSharp.Extensions.LibHac;

public class LibHacRomProvider : IDisposable
{
    public const ulong EX_KING_APP_ID = 0x0100F2C0115B6000;

    private SwitchFs? _baseFs;
    private SwitchFs? _updateFs;
    private IFileSystem? _fileSystem;
    private IDisposable? _helper;

    public TkRom CreateRom(
        TkChecksums checksums,
        KeySet keys,
        RomSource baseSource,
        string basePath,
        RomSource updateSource,
        string updatePath)
    {
        if (baseSource == RomSource.SdCard && updateSource == RomSource.SdCard && basePath == updatePath) {
            _helper = new SdRomHelper();
            _baseFs = ((SdRomHelper)_helper).Initialize(basePath, keys);
            _fileSystem = InitializeLayeredFs(_baseFs, _baseFs);
            return new TkRom(checksums, _fileSystem);
        }
        else {
            _baseFs = CreateSwitchFs(baseSource, basePath, keys);
            _updateFs = CreateSwitchFs(updateSource, updatePath, keys);
            _fileSystem = InitializeLayeredFs(_baseFs, _updateFs);
            return new TkRom(checksums, _fileSystem);
        }
    }

    private SwitchFs CreateSwitchFs(RomSource source, string path, KeySet keys)
    {
        _helper = source switch
        {
            RomSource.File => new FileRomHelper(),
            RomSource.SdCard => new SdRomHelper(),
            RomSource.SplitFiles => new SplitRomHelper(),
            _ => throw new ArgumentException($"Invalid source: {source}")
        };

        return ((dynamic)_helper).Initialize(path, keys);
    }

    public static IFileSystem InitializeLayeredFs(SwitchFs baseFs, SwitchFs updateFs)
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
    }

    public enum RomSource
    {
        File,      // XCI or NSP file
        SdCard,    // From SD card
        SplitFiles // Split files in a directory
    }
} 