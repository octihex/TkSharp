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

namespace TkSharp.Extensions.LibHac;

public static class TkRomHelper
{
    public const ulong EX_KING_APP_ID = 0x0100F2C0115B6000;

    public static SwitchFs InitializeFromFile(IStorage storage, string filePath, KeySet keys)
    {
        return storage.GetSwitchFs(filePath, keys);
    }

    public static SwitchFs InitializeFromSdCard(string sdCardPath, KeySet keys)
    {
        LocalFileSystem.Create(out var localFs, sdCardPath).ThrowIfFailure();
        var localFsRef = new UniqueRef<IAttributeFileSystem>(localFs);

        var concatFs = new ConcatenationFileSystem(ref localFsRef);

        using var contentDirPath = new global::LibHac.Fs.Path();
        PathFunctions.SetUpFixedPath(ref contentDirPath.Ref(), "/Nintendo/Contents"u8).ThrowIfFailure();

        var contentDirFs = new SubdirectoryFileSystem(concatFs);
        contentDirFs.Initialize(in contentDirPath).ThrowIfFailure();

        var encFs = new AesXtsFileSystem(contentDirFs, keys.SdCardEncryptionKeys[1].DataRo.ToArray(), 0x4000);
        return new SwitchFs(keys, encFs, null);
    }

    public static (IStorage Storage, SwitchFs SwitchFs) InitializeFromSplitFiles(string splitDirectory, KeySet keys)
    {
        var splitFiles = Directory.GetFiles(splitDirectory)
            .OrderBy(f => f)
            .Select(f => new LocalStorage(f, FileAccess.Read))
            .ToArray();

        var storage = new ConcatenationStorage(splitFiles, true);
        var switchFs = storage.GetSwitchFs("rom", keys);

        return (storage, switchFs);
    }

    public static IFileSystem InitializeFileSystem(SwitchFs baseFs, SwitchFs updateFs)
    {
        return baseFs.Applications[EX_KING_APP_ID].Main.MainNca.Nca
            .OpenFileSystemWithPatch(updateFs.Applications[EX_KING_APP_ID].Patch.MainNca.Nca, 
                NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
    }

    public static TkRom CreateRom(
        TkChecksums checksums,
        KeySet keys,
        RomSource baseSource,
        string basePath,
        RomSource updateSource,
        string updatePath)
    {
        // Initialize base game
        var baseFs = baseSource switch {
            RomSource.File => InitializeFromFile(new LocalStorage(basePath, FileAccess.Read), basePath, keys),
            RomSource.SdCard => InitializeFromSdCard(basePath, keys),
            RomSource.SplitFiles => InitializeFromSplitFiles(basePath, keys).SwitchFs,
            _ => throw new ArgumentException("Invalid base source")
        };

        // Initialize update
        var updateFs = updateSource switch {
            RomSource.File => InitializeFromFile(new LocalStorage(updatePath, FileAccess.Read), updatePath, keys),
            RomSource.SdCard => InitializeFromSdCard(updatePath, keys),
            RomSource.SplitFiles => InitializeFromSplitFiles(updatePath, keys).SwitchFs,
            _ => throw new ArgumentException("Invalid update source")
        };

        // Create patched filesystem
        var fileSystem = InitializeFileSystem(baseFs, updateFs);

        return new TkRom(checksums, fileSystem);
    }

    public enum RomSource
    {
        File,      // XCI or NSP file
        SdCard,    // From SD card
        SplitFiles // Split files in a directory
    }
} 