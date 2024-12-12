using BymlLibrary;

namespace TkSharp.Merging.Mergers.GameData;

public class GameDataMergeTrackingEntry(Byml baseEntry, bool isStructTable)
{
    public Byml BaseEntry { get; set; } = baseEntry;

    public bool IsStructTable { get; set; } = isStructTable;

    public List<Byml> Changes { get; set; } = [];
}