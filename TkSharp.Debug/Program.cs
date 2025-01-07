using System.Diagnostics;
using TkSharp;
using TkSharp.Core;
using TkSharp.Debug;
using TkSharp.IO.Writers;
using TkSharp.Merging;

// Debug cleanup
string outputFolderPath = Path.Combine(AppContext.BaseDirectory, ".data", "output");
if (Directory.Exists(outputFolderPath)) {
    Directory.Delete(outputFolderPath, recursive: true);
}

// Initialize mod manager
var manager = TkModManager.CreatePortable();

// Create merged output writer
ITkModWriter writer = new FolderModWriter(
    outputFolderPath);

// Create merger
TkMerger merger = new(writer, DebugRomProvider.Instance.GetRom());

long startTime = Stopwatch.GetTimestamp();

// Merge selected mods
// TODO: Select options as well here
merger.Merge(
    manager
        .GetCurrentProfile()
        .Mods
        .Select(x => x.Mod.Changelog)
);

TimeSpan delta = Stopwatch.GetElapsedTime(startTime);
Console.WriteLine($"Elapsed time: {delta.TotalMilliseconds} ms");
