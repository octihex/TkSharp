// using System.Diagnostics;
// using TkSharp;
// using TkSharp.Core;
// using TkSharp.Core.IO;
// using TkSharp.Data.Embedded;
// using TkSharp.Merging;
//
// string inputModFolder = args[0];
// string outputModFolder = args[1];
//
// TkModManager manager = TkModManager.Portable;
//
// var modId = Ulid.NewUlid();
//
// ITkModSource source = new FolderModSource(inputModFolder);
// ITkModWriter writer = manager.GetSystemModWriter(modId);
// ITkRom rom = new ExtractedTkRom(@"F:\Games\RomFS\Totk\1.2.1",
//     TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin())
// );
//
// TkChangelogBuilder builder = new(
//     source, writer, rom
// );
//
// var stopwatch = Stopwatch.StartNew();
//
// TkChangelog changelog = builder.Build();
//
// stopwatch.Stop();
// Console.WriteLine(stopwatch.ElapsedMilliseconds);
//
// TkMod mod = new() {
//     Id = modId,
//     Name = "Randomizer",
//     Author = "Me",
//     Changelog = changelog
// };
//
// manager.Add(mod);
// manager.Save();

using TkSharp;

TkModManager manager = TkModManager.Portable;
manager.Save();
