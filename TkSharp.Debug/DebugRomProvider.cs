using TkSharp.Core;
using TkSharp.Core.Common;
using TkSharp.Core.IO;
using TkSharp.Data.Embedded;

namespace TkSharp.Debug;

public sealed class DebugRomProvider : Singleton<DebugRomProvider>, ITkRomProvider
{
    public ITkRom GetRom()
    {
        // return new ExtractedTkRom(@"F:\Games\RomFS\Totk\1.2.1",
        //     TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin())
        // );

        // return new PackedTkRom(
        //     TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin()),
        //     @"C:\Users\ArchLeaders\AppData\Roaming\Ryujinx\system",
        //     @"D:\Games\Emulation\Packaged\Tears-of-the-Kingdom\TotK-1.0.0.xci",
        //     @"D:\Games\Emulation\Packaged\Tears-of-the-Kingdom\TotK-1.2.1.nsp");

        return new SdCardTkRom(
            TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin()),
            @"F:\switch",
            @"F:\");

    }
}