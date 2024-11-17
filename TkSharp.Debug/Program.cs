using System.Diagnostics;
using TkSharp.Core;
using TkSharp.Core.IO;
using TkSharp.Data.Embedded;
using TkSharp.Merging;

string inputModFolder = args[0];
string outputModFolder = args[1];

ITkModSource source = new FolderModSource(inputModFolder);
ITkModWriter writer = new FolderModWriter(outputModFolder);
ITkRom rom = new ExtractedTkRom(@"F:\Games\RomFS\Totk\1.2.1",
    TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin())
);

TkChangelogBuilder builder = new(
    source, writer, rom
);

var stopwatch = Stopwatch.StartNew();

TkChangelog changelog = builder.Build();

stopwatch.Stop();
Console.WriteLine(stopwatch.ElapsedMilliseconds);