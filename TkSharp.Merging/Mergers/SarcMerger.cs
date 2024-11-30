using CommunityToolkit.HighPerformance.Buffers;
using SarcLibrary;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.ResourceSizeTable;

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
        
        foreach (RentedBuffers<byte>.Entry input in inputs) {
            Sarc changelog = Sarc.FromBinary(input.Segment);
            MergeEntry(merged, changelog);
        }
        
        WriteOutput(entry, merged, output);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Sarc merged = Sarc.FromBinary(@base);
        Sarc changelog = Sarc.FromBinary(input);  
        MergeEntry(merged, changelog);
        WriteOutput(entry, merged, output);
    }

    private void MergeEntry(in Sarc merged, Sarc changelog)
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
            
            using Stream output = merged.OpenWrite(name);
            merger.MergeSingle(_fakeEntry, data, vanillaData, output);
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