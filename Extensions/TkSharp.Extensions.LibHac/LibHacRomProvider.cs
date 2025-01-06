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

public class LibHacRomProvider
{
    public const ulong EX_KING_APP_ID = 0x0100F2C0115B6000;

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
            using SdRomHelper sdHelper = new SdRomHelper();
            using SwitchFs sdFs = sdHelper.Initialize(basePath, keys);
            using IFileSystem fileSystem = InitializeLayeredFs(sdFs, sdFs);
            return new TkRom(checksums, fileSystem);
        }
        else
        {
            using SwitchFs baseFs = CreateSwitchFs(baseSource, basePath, keys);
            using SwitchFs updateFs = CreateSwitchFs(updateSource, updatePath, keys);
            using IFileSystem fileSystem = InitializeLayeredFs(baseFs, updateFs);
            return new TkRom(checksums, fileSystem);
        }
    }

    private SwitchFs CreateSwitchFs(RomSource source, string path, KeySet keys)
    {
        return source switch
        {
            RomSource.File => new FileRomHelper().Initialize(path, keys),
            RomSource.SdCard => new SdRomHelper().Initialize(path, keys),
            RomSource.SplitFiles => new SplitRomHelper().Initialize(path, keys),
            _ => throw new ArgumentException($"Invalid source: {source}")
        };
    }

    public static IFileSystem InitializeLayeredFs(SwitchFs baseFs, SwitchFs updateFs)
    {
        return baseFs.Applications[EX_KING_APP_ID].Main.MainNca.Nca
            .OpenFileSystemWithPatch(updateFs.Applications[EX_KING_APP_ID].Patch.MainNca.Nca,
                NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
    }

    public enum RomSource
    {
        File,      // XCI or NSP file
        SdCard,    // From SD card
        SplitFiles // Split files in a directory
    }
} 