using TkSharp.Core;
using TkSharp.Core.IO;
using TkSharp.Data.Embedded;
using TkSharp.Extensions.LibHac;
using Config = TkSharp.DevTools.ViewModels.SettingsPageViewModel;

namespace TkSharp.DevTools.Helpers;

public class RomHelper
{
    private static readonly TkChecksums _checksums = TkChecksums.FromStream(
        TkEmbeddedDataSource.GetChecksumsBin());
    
    public static ITkRom GetRom()
    {
        if (Config.Shared.GameDumpFolderPath is string gamePath && Directory.Exists(gamePath)) {
            return new ExtractedTkRom(gamePath, _checksums);
        }

        if (Config.Shared.KeysFolderPath is string keysFolderPath
            && Config.Shared.BaseGameFilePath is string baseGameFilePath
            && Config.Shared.GameUpdateFilePath is string gameUpdateFilePath) {
            return new PackedTkRom(_checksums, keysFolderPath, baseGameFilePath, gameUpdateFilePath);
        }

        throw new InvalidOperationException("Invalid configuration.");
    }
}