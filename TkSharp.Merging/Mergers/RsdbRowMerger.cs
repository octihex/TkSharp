using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance.Buffers;
using Revrs;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.Mergers.ResourceDatabase;

namespace TkSharp.Merging.Mergers;

public sealed class RsdbRowMergers(TkZstd zs)
{
    public readonly RsdbRowMerger RowId = new("__RowId", zs);
    public readonly RsdbRowMerger Name = new("Name", zs);
    public readonly RsdbRowMerger FullTagId = new("FullTagId", zs);
    public readonly RsdbRowMerger NameHash = new("NameHash", zs);
}

public sealed class RsdbRowMerger(string keyName, TkZstd zs) : ITkMerger
{
    private readonly string _keyName = keyName;
    private readonly TkZstd _zs = zs;
    private readonly RsdbRowComparer _rowComparer = new(keyName);

    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlArray rows = merged.GetArray();

        foreach (RentedBuffers<byte>.Entry input in inputs) {
            Byml changelog = Byml.FromBinary(input.Span);
            switch (changelog.Value) {
                case IDictionary<uint, Byml> hashMap32:
                    MergeHashMap32(hashMap32, rows);
                    break;
                case IDictionary<string, Byml> map:
                    MergeMap(map, rows);
                    break;
            }
        }
        
        rows.Sort(_rowComparer);
        WriteOutput(entry, merged, endianness, version, output);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        throw new NotImplementedException();
    }

    private void WriteOutput(TkChangelogEntry entry, Byml merged, Endianness endianness, ushort version, Stream output)
    {
        using MemoryStream ms = new();
        merged.WriteBinary(ms, endianness, version);

        if (!ms.TryGetBuffer(out ArraySegment<byte> buffer)) {
            buffer = ms.ToArray();
        }

        if (!entry.Attributes.HasFlag(TkFileAttributes.HasZsExtension)) {
            output.Write(buffer);
            return;
        }

        using SpanOwner<byte> compressed = SpanOwner<byte>.Allocate(buffer.Count);
        int compressedSize = _zs.Compress(buffer, compressed.Span, entry.ZsDictionaryId);
        output.Write(compressed.Span[..compressedSize]);
    }

    private void MergeMap(IDictionary<string, Byml> changelog, BymlArray @base)
    {
        foreach (Byml entry in @base) {
            BymlMap baseMap = entry.GetMap();
            string key = baseMap[_keyName].GetString();

            if (!changelog.Remove(key, out Byml? changelogEntry)) {
                continue;
            }
            
            BymlMerger.MergeMap(baseMap, changelogEntry.GetMap());
        }

        foreach ((_, Byml value) in changelog) {
            @base.Add(value);
        }
    }

    private void MergeHashMap32(IDictionary<uint, Byml> changelog, BymlArray @base)
    {
        foreach (Byml entry in @base) {
            BymlMap baseMap = entry.GetMap();
            uint key = baseMap[_keyName].GetUInt32();

            if (!changelog.Remove(key, out Byml? changelogEntry)) {
                continue;
            }
            
            BymlMerger.MergeMap(baseMap, changelogEntry.GetMap());
        }

        foreach ((_, Byml value) in changelog) {
            @base.Add(value);
        }
    }
}