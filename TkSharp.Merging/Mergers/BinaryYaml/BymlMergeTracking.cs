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
        int newEntryOffset = 0;
        
        foreach (int i in entry.Removals) {
            @base[i] = BymlChangeType.Remove;
        }

        foreach (IGrouping<int, Byml> additions in entry.Additions.SelectMany(x => x).GroupBy(x => x.InsertIndex, x => x.Entry).OrderBy(x => x.Key)) {
            ProcessAdditions(ref newEntryOffset, @base, additions);
        }

        for (int i = 0; i < @base.Count; i++) {
            if (@base[i].Value is not BymlChangeType.Remove) {
                continue;
            }
            
            @base.RemoveAt(i);
            i--;
        }
    }

    private static void ProcessAdditions(ref int newEntryOffset, BymlArray @base, IGrouping<int, Byml> additions)
    {
        foreach (Byml addition in additions) {
            int relativeIndex = additions.Key + newEntryOffset;

            if (@base.Count > relativeIndex) {
                @base.Insert(additions.Key + newEntryOffset, addition);
            }
            else {
                @base.Add(addition);
            }
            
            newEntryOffset++;
        }
    }
}