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
    private static readonly TkChecksums _checksums = TkChecksums.FromStream(
        TkEmbeddedDataSource.GetChecksumsBin());
    
    public static readonly RomHelper Instance = new();
    
    public ITkRom GetRom()
    {
        if (Config.Shared.GameDumpFolderPath is string gamePath && Directory.Exists(gamePath)) {
            return new ExtractedTkRom(gamePath, _checksums);
        }

        if (Config.Shared.KeysFolderPath is string keysFolderPath
            && GetKeys(keysFolderPath) is KeySet keys 
            && Config.Shared.BaseGameFilePath is string baseGameFilePath
            && Config.Shared.GameUpdateFilePath is string gameUpdateFilePath) {
            return new PackedTkRom(_checksums, keys, baseGameFilePath, gameUpdateFilePath);
        }

        if (Config.Shared.KeysFolderPath is string sdKeysFolderPath
            && Config.Shared.SdCardContentsPath is string contentsPath
            && Directory.Exists(contentsPath)) {
            return new SdCardTkRom(_checksums, sdKeysFolderPath, contentsPath);
        }

        throw new InvalidOperationException("Invalid configuration.");
    }
    
    public static KeySet? GetKeys(string keysFolder)
    {
        string prodKeysFilePath = Path.Combine(keysFolder, "prod.keys");
        if (!File.Exists(prodKeysFilePath)) {
            TkLog.Instance.LogError("A 'prod.keys' file could not be found in '{KeysFolder}'", keysFolder);
            return null;
        }
        
        string titleKeysFilePath = Path.Combine(keysFolder, "title.keys");
        if (!File.Exists(titleKeysFilePath)) {
            TkLog.Instance.LogError("A 'title.keys' file could not be found in '{KeysFolder}'", keysFolder);
            return null;
        }

        KeySet keys = new();
        ExternalKeyReader.ReadKeyFile(keys,
            prodKeysFilename: prodKeysFilePath,
            titleKeysFilename: titleKeysFilePath);

        return keys;
    }
}