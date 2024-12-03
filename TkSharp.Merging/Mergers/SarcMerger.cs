using CommunityToolkit.HighPerformance.Buffers;
using SarcLibrary;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.ResourceSizeTable;
using NestedMergeTarget = (TkSharp.Merging.ITkMerger Merger, System.ArraySegment<byte> VanillaData, System.Collections.Generic.List<System.ArraySegment<byte>> Targets); 

namespace TkSharp.Merging.Mergers;

public sealed class SarcMerger(TkMerger masterMerger, TkResourceSizeCollector resourceSizeCollector, TkZstd zs) : ITkMerger
{
    private readonly TkMerger _masterMerger = masterMerger;
    private readonly TkResourceSizeCollector _resourceSizeCollector = resourceSizeCollector;
    private readonly TkZstd _zs = zs;
    private readonly TkChangelogEntry _fakeEntry = new(
        string.Empty,
        ChangelogEntryType.Changelog,
        TkFileAttributes.None,
        zsDictionaryId: -1);

    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Sarc merged = Sarc.FromBinary(vanillaData);
        Dictionary<string, NestedMergeTarget> mergeTargets = [];
        
        foreach (RentedBuffers<byte>.Entry input in inputs) {
            Sarc changelog = Sarc.FromBinary(input.Segment);
            MergeEntry(merged, changelog, mergeTargets);
        }

        MergeNestedTargets(merged, mergeTargets);
        
        WriteOutput(entry, merged, output);
    }

    public void Merge(TkChangelogEntry entry, IEnumerable<ArraySegment<byte>> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Sarc merged = Sarc.FromBinary(vanillaData);
        Dictionary<string, NestedMergeTarget> mergeTargets = [];
        
        foreach (ArraySegment<byte> input in inputs) {
            Sarc changelog = Sarc.FromBinary(input);
            MergeEntry(merged, changelog, mergeTargets);
        }
        
        MergeNestedTargets(merged, mergeTargets);
        
        WriteOutput(entry, merged, output);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Sarc merged = Sarc.FromBinary(@base);
        Sarc changelog = Sarc.FromBinary(input);  
        MergeEntry(merged, changelog, mergeTargets: null);
        WriteOutput(entry, merged, output);
    }

    private void MergeNestedTargets(Sarc merged, Dictionary<string, NestedMergeTarget> mergeTargets)
    {
        foreach ((string name, NestedMergeTarget target) in mergeTargets) {
            using Stream nestedOutput = merged.OpenWrite(name);
            _fakeEntry.Canonical = name;
            
            switch (target.Targets.Count) {
                case >1:
                    target.Merger.Merge(_fakeEntry, target.Targets, target.VanillaData, nestedOutput);
                    break;
                default:
                    target.Merger.MergeSingle(_fakeEntry, target.Targets[0], target.VanillaData, nestedOutput);
                    break;
            }
        }
    }

    private void MergeEntry(in Sarc merged, Sarc changelog, Dictionary<string, NestedMergeTarget>? mergeTargets)
    {
        foreach ((string name, ArraySegment<byte> data) in changelog) {
            if (_masterMerger.GetMerger(name) is not ITkMerger merger) {
                merged[name] = data;
                continue;
            }
            
            if (!merged.TryGetValue(name, out ArraySegment<byte> vanillaData)) {
                merged[name] = data;
                continue;
            }

            _fakeEntry.Canonical = name;

            if (mergeTargets is null) {
                using Stream output = merged.OpenWrite(name);
                merger.MergeSingle(_fakeEntry, data, vanillaData, output);
                continue;
            }

            if (!mergeTargets.TryGetValue(name, out NestedMergeTarget target)) {
                mergeTargets[name] = (
                    merger,
                    vanillaData,
                    Targets: [data]
                );
                
                continue;
            }
            
            target.Targets.Add(data);
        }
    }

    private void WriteOutput(TkChangelogEntry entry, Sarc merged, Stream output)
    {
        using MemoryStream ms = new();
        merged.Write(ms);

        if (!ms.TryGetBuffer(out ArraySegment<byte> buffer)) {
            buffer = ms.ToArray();
        }

        if (entry.Attributes.HasFlag(TkFileAttributes.HasZsExtension)) {
            using SpanOwner<byte> compressed = SpanOwner<byte>.Allocate(buffer.Count);
            int compressedSize = _zs.Compress(buffer, compressed.Span, entry.ZsDictionaryId);
            output.Write(compressed.Span[..compressedSize]);
            return;
        }
        
        output.Write(buffer);
    }
}