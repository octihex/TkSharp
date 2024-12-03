using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.Mergers.BinaryYaml;

public class BymlMergeTracking : Dictionary<BymlArray, BymlMergeTrackingEntry>
{
    public void Apply()
    {
        foreach ((BymlArray @base, BymlMergeTrackingEntry entry) in this) {
            ApplyEntry(@base, entry);
        }
    }
    
    private static void ApplyEntry(BymlArray @base, BymlMergeTrackingEntry entry)
    {
        foreach (int i in entry.Removals) {
            @base[i] = BymlChangeType.Remove;
        }

        foreach ((int InsertIndex, Byml Entry) addition in entry.Additions.OrderBy(additionEntry => additionEntry.InsertIndex)) {
            @base.Insert(addition.InsertIndex, addition.Entry);
        }

        for (int i = 0; i < @base.Count; i++) {
            if (@base[i].Value is not BymlChangeType.Remove) {
                continue;
            }
            
            @base.RemoveAt(i);
            i--;
        }
    }
}