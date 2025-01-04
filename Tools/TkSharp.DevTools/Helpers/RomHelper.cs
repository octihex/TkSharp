using LibHac.Common.Keys;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.IO;
using TkSharp.Data.Embedded;
using TkSharp.Extensions.LibHac;
using Config = TkSharp.DevTools.ViewModels.SettingsPageViewModel;

namespace TkSharp.DevTools.Helpers;

public class RomHelper : ITkRomProvider
{
    private static readonly TkChecksums _checksums = TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin());
    
    public static readonly RomHelper Instance = new();

    public ITkRom GetRom()
    {
        if (Config.Shared.GameDumpFolderPath is string gamePath && Directory.Exists(gamePath)) {
            return new ExtractedTkRom(gamePath, _checksums);
        }

        if (Config.Shared.KeysFolderPath is not string keysFolderPath) {
            throw new InvalidOperationException("Keys folder path is required but not configured.");
        }

        string prodKeysPath = Path.Combine(keysFolderPath, "prod.keys");
        if (!File.Exists(prodKeysPath)) {
            throw new FileNotFoundException($"A 'prod.keys' file could not be found in '{keysFolderPath}'");
        }

        string titleKeysPath = Path.Combine(keysFolderPath, "title.keys");
        if (!File.Exists(titleKeysPath)) {
            throw new FileNotFoundException($"A 'title.keys' file could not be found in '{keysFolderPath}'");
        }

        var keys = new KeySet();
        ExternalKeyReader.ReadKeyFile(keys, prodKeysFilename: prodKeysPath, titleKeysFilename: titleKeysPath);

        var (baseSource, basePath) = GetRomSource();
        var (updateSource, updatePath) = GetUpdateSource();

        if (baseSource is null || basePath is null || updateSource is null || updatePath is null) {
            throw new InvalidOperationException("Invalid configuration: ROM source or path is not set.");
        }

        Console.WriteLine($"Reading base game from {baseSource} and update from {updateSource}");

        using (var romProvider = new LibHacRomProvider())
        {
            return romProvider.CreateRom(
                _checksums,
                keys,
                baseSource.Value, basePath,
                updateSource.Value, updatePath);
        }
    }

    private static (LibHacRomProvider.RomSource? Source, string? Path) GetRomSource()
    {
        if (Config.Shared.BaseGameFilePath is string path && File.Exists(path))
            return (LibHacRomProvider.RomSource.File, path);
        if (Config.Shared.SplitFilesPath is string splitPath && Directory.Exists(splitPath))
            return (LibHacRomProvider.RomSource.SplitFiles, splitPath);
        if (Config.Shared.SdCardRootPath is string sdPath && Directory.Exists(sdPath))
            return (LibHacRomProvider.RomSource.SdCard, sdPath);
        return (null, null);
    }

    private static (LibHacRomProvider.RomSource? Source, string? Path) GetUpdateSource()
    {
        if (Config.Shared.GameUpdateFilePath is string path && File.Exists(path))
            return (LibHacRomProvider.RomSource.File, path);
        if (Config.Shared.SdCardRootPath is string sdPath && Directory.Exists(sdPath))
            return (LibHacRomProvider.RomSource.SdCard, sdPath);
        return (null, null);
    }
}