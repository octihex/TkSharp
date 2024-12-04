using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Revrs;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.Mergers.BinaryYaml;
using TkSharp.Merging.Mergers.ResourceDatabase;

namespace TkSharp.Merging.Mergers;

public static class RsdbRowMergers
{
    public static readonly RsdbRowMerger RowId = new("__RowId");
    public static readonly RsdbRowMerger Name = new("Name");
    public static readonly RsdbRowMerger FullTagId = new("FullTagId");
    public static readonly RsdbRowMerger NameHash = new("NameHash");
}

public sealed class RsdbRowMerger(string keyName) : ITkMerger
{
    private readonly string _keyName = keyName;
    private readonly RsdbRowComparer _rowComparer = new(keyName);

    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlArray rows = merged.GetArray();
        BymlMergeTracking tracking = new();

        foreach (RentedBuffers<byte>.Entry input in inputs) {
            MergeEntry(rows, input.Span, tracking);
        }

        tracking.Apply();

        rows.Sort(_rowComparer);
        merged.WriteBinary(output, endianness, version);
    }

    public void Merge(TkChangelogEntry entry, IEnumerable<ArraySegment<byte>> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlArray rows = merged.GetArray();
        BymlMergeTracking tracking = new();

        foreach (ArraySegment<byte> input in inputs) {
            MergeEntry(rows, input, tracking);
        }

        tracking.Apply();

        rows.Sort(_rowComparer);
        merged.WriteBinary(output, endianness, version);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Byml merged = Byml.FromBinary(@base, out Endianness endianness, out ushort version);
        BymlArray rows = merged.GetArray();
        BymlMergeTracking tracking = new();
        MergeEntry(rows, input, tracking);
        tracking.Apply();
        rows.Sort(_rowComparer);
        merged.WriteBinary(output, endianness, version);
    }

    private void MergeEntry(BymlArray rows, Span<byte> input, BymlMergeTracking tracking)
    {
        Byml changelog = Byml.FromBinary(input);

        switch (changelog.Value) {
            case IDictionary<uint, Byml> hashMap32:
                MergeHashMap32(hashMap32, rows, tracking);
                break;
            case IDictionary<string, Byml> map:
                MergeMap(map, rows, tracking);
                break;
        }
    }

    private void MergeMap(IDictionary<string, Byml> changelog, BymlArray @base, BymlMergeTracking tracking)
    {
        foreach (Byml entry in @base) {
            BymlMap baseMap = entry.GetMap();
            string key = baseMap[_keyName].GetString();

            if (!changelog.Remove(key, out Byml? changelogEntry)) {
                continue;
            }

            BymlMerger.MergeMap(baseMap, changelogEntry.GetMap(), tracking);
        }

        foreach ((_, Byml value) in changelog) {
            @base.Add(value);
        }
    }

    private void MergeHashMap32(IDictionary<uint, Byml> changelog, BymlArray @base, BymlMergeTracking tracking)
    {
        foreach (Byml entry in @base) {
            BymlMap baseMap = entry.GetMap();
            uint key = baseMap[_keyName].GetUInt32();

            if (!changelog.Remove(key, out Byml? changelogEntry)) {
                continue;
            }

            BymlMerger.MergeMap(baseMap, changelogEntry.GetMap(), tracking);
        }

        foreach ((_, Byml value) in changelog) {
            @base.Add(value);
        }
    }
}