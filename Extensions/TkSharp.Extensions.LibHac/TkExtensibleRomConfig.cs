using LibHac.Common.Keys;
using TkSharp.Extensions.LibHac.Models;
using TkSharp.Extensions.LibHac.Util;

namespace TkSharp.Extensions.LibHac;

internal struct TkExtensibleRomConfig
{
    public TkExtensibleConfig<string> PreferredVersion = new(TkExtensibleConfigType.None);
    
    public TkExtensibleConfig<string> KeysFolder = new(TkExtensibleConfigType.Folder);
    
    public TkExtensibleConfig<IEnumerable<string>> ExtractedGameDumpFolderPath = new(TkExtensibleConfigType.Folder);
    
    public TkExtensibleConfig<string> SdCard = new(TkExtensibleConfigType.Folder, CheckSdCard);
    
    public TkExtensibleConfig<IEnumerable<string>> PackagedBaseGame = new(TkExtensibleConfigType.Path, CheckPackagedFile);
    
    public TkExtensibleConfig<IEnumerable<string>> PackagedUpdate = new(TkExtensibleConfigType.Path, CheckPackagedFile);

    public TkExtensibleRomConfig()
    {
    }

    private static bool CheckPackagedFile(IEnumerable<string> values, KeySet keys, SwitchFsContainer? switchFsContainer)
    {
        bool result = false;
        bool hasUpdate = false;

        foreach (string path in values) {
            result = TkGameRomUtils.IsValid(keys, path, out bool hasUpdateInline, switchFsContainer);
            if (!hasUpdate) hasUpdate = hasUpdateInline;
        }
        
        return result || hasUpdate;
    }

    private static bool CheckSdCard(string value, KeySet keys, SwitchFsContainer? switchFsContainer)
    {
        bool result = TkSdCardUtils.CheckSdCard(keys, value, out bool hasUpdate, switchFsContainer);
        return result || hasUpdate;
    }
}