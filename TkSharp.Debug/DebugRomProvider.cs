using LibHac.Common.Keys;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using TkSharp.Core;
using TkSharp.Core.Common;
using TkSharp.Data.Embedded;
using TkSharp.Extensions.LibHac;

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

        var keys = new KeySet();
        ExternalKeyReader.ReadKeyFile(keys,
            prodKeysFilename: @"F:\switch\prod.keys",
            titleKeysFilename: @"F:\switch\title.keys"
        );

        string splitDirectory = @"C:\Games\Switch games\TOTKSPLIT";
        var splitFiles = Directory.GetFiles(splitDirectory)
            .OrderBy(f => f) 
            .Select(f => new LocalStorage(f, FileAccess.Read))
            .ToArray();

        var concatStorage = new ConcatenationStorage(splitFiles, true);

        return new HybridTkRom(
            TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin()),
            keys,
            concatStorage,
            @"F:\",
            false
        );
    }
}