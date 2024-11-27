using TkSharp;
using TkSharp.Core;
using TkSharp.Debug;
using TkSharp.Debug.IO;
using TkSharp.Merging;

string outputModFolder = args[1];

// Initialize mod manager
var manager = TkModManager.CreatePortable();

// Create merged output writer
ITkModWriter writer = new FolderModWriter(outputModFolder);

// Create merger
TkMerger merger = new(writer, DebugRomProvider.Instance.GetRom());

// Merge selected mods
// TODO: Select options as well here
merger.Merge(
    manager
        .GetCurrentProfile()
        .Mods
        .Select(x => x.Mod.Changelog)
);
