using TkSharp.Core;
using TkSharp.Core.Common;
using TkSharp.Core.IO;
using TkSharp.Data.Embedded;

namespace TkSharp.Debug;

public sealed class DebugRomProvider : Singleton<DebugRomProvider>, ITkRomProvider
{
    public ITkRom GetRom()
    {
        return new ExtractedTkRom(@"F:\Games\RomFS\Totk\1.2.1",
            TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin())
        );
    }
}