using System.Text.Json;
using TkSharp.Core;
using TkSharp.Core.IO;
using TkSharp.Data.Embedded;

namespace TkSharp.DevTools.Helpers;

public class RomHelper
{
    private static readonly TkChecksums _checksums = TkChecksums.FromStream(
        TkEmbeddedDataSource.GetChecksumsBin());
    
    private static readonly string _tkConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "totk", "Config.json");
    
    public static ITkRom GetRom()
    {
        if (!File.Exists(_tkConfigPath)) {
            throw new Exception("The TotK configuration file does not exist.");
        }
        
        using Stream fs = File.OpenRead(_tkConfigPath);
        if (JsonSerializer.Deserialize<TkConfig>(fs) is not TkConfig config) {
            throw new Exception("The TotK configuration file was invalid.");
        }
        
        return new ExtractedTkRom(config.GamePath, _checksums);
    }

    private record TkConfig(string GamePath);
}