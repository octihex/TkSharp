using BymlLibrary;

namespace TkSharp.Merging.Mergers.BinaryYaml;

public class BymlMergeTrackingEntry
{
    public List<(int InsertIndex, Byml Entry)> Additions { get; } = [];

    public HashSet<int> Removals { get; } = [];
}