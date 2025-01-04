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
    private SwitchFs _baseFs;
    private SwitchFs _updateFs;

    public TkRom CreateRom(
        TkChecksums checksums,
        KeySet keys,
        RomSource baseSource,
        string basePath,
        RomSource updateSource,
        string updatePath)
    {
        if (baseSource == RomSource.SdCard && updateSource == RomSource.SdCard && basePath == updatePath)
        {
            using var sdHelper = new SdRomHelper();
            var sdFs = sdHelper.InitializeFromSdCard(basePath, keys);
            var fileSystem = InitializeFileSystem(sdFs, sdFs);
            return new TkRom(checksums, fileSystem);
        }
        else
        {
            _baseFs = InitializeFs(baseSource, basePath, keys);
            _updateFs = InitializeFs(updateSource, updatePath, keys);
            var fileSystem = InitializeFileSystem(_baseFs, _updateFs);
            return new TkRom(checksums, fileSystem);
        }
    }

    private SwitchFs InitializeFs(RomSource source, string path, KeySet keys)
    {
        switch (source)
        {
            case RomSource.File:
                var fileHelper = new FileRomHelper();
                return fileHelper.InitializeFromFile(path, keys);
            case RomSource.SdCard:
                var sdHelper = new SdRomHelper();
                return sdHelper.InitializeFromSdCard(path, keys);
            case RomSource.SplitFiles:
                var splitHelper = new SplitRomHelper();
                return splitHelper.InitializeFromSplitFiles(path, keys);
            default:
                throw new ArgumentException($"Invalid source: {source}");
        }
    }

    public static IFileSystem InitializeFileSystem(SwitchFs baseFs, SwitchFs updateFs)
    {
        return baseFs.Applications[EX_KING_APP_ID].Main.MainNca.Nca
            .OpenFileSystemWithPatch(updateFs.Applications[EX_KING_APP_ID].Patch.MainNca.Nca,
                NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
    }

    public enum RomSource {
        File,      // XCI or NSP file
        SdCard,    // From SD card
        SplitFiles // Split files in a directory
    }

    public void Dispose()
    {
        _baseFs?.Dispose();
        _updateFs?.Dispose();
    }
} 