using System.Diagnostics.CodeAnalysis;
using LibHac.Common.Keys;

namespace TkSharp.Extensions.LibHac.Util;

public static class TkKeyUtils
{
    private static readonly string[] _possibleKeyLocations = [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch")
    ];
    
    public static bool TryGetKeys([MaybeNullWhen(false)] out KeySet keys) => TryGetKeys(sdCardRootPath: null, out keys);

    public static bool TryGetKeys(string? sdCardRootPath, [MaybeNullWhen(false)] out KeySet keys)
    {
        if (sdCardRootPath is not null && GetKeys(sdCardRootPath) is KeySet keysFromSdCard) {
            keys = keysFromSdCard;
            return true;
        }

        foreach (string possibleKeyLocation in _possibleKeyLocations) {
            if (!Directory.Exists(possibleKeyLocation) || GetKeysFromFolder(possibleKeyLocation) is not KeySet roamingKeys) {
                continue;
            }

            keys = roamingKeys;
            return true;
        }

        keys = null;
        return false;
    }
    
    public static KeySet? GetKeysFromFolder(string target)
    {
        string keysFile = Path.Combine(target, "prod.keys");
        if (!File.Exists(keysFile)) {
            return null;
        }
        
        string titleKeysFile = Path.Combine(target, "title.keys");
        if (File.Exists(titleKeysFile)) {
            KeySet keys = new();
            ExternalKeyReader.ReadKeyFile(keys, titleKeysFilename: titleKeysFile, prodKeysFilename: keysFile);
            return keys;
        }

        KeySet keys = new();
        ExternalKeyReader.ReadKeyFile(keys, prodKeysFilename: keysFile);
        return keys;
    }
    
    private static KeySet? GetKeys(string sdCardRootPath)
    {
        string switchFolder = Path.Combine(sdCardRootPath, "switch");
        return GetKeysFromFolder(switchFolder);
    }
}