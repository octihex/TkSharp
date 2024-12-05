using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Revrs;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.Mergers.BinaryYaml;
using TkSharp.Merging.Mergers.GameData;

namespace TkSharp.Merging.Mergers;

public sealed class GameDataMerger : Singleton<GameDataMerger>, ITkMerger
{
    private static BymlRowComparer _rowComparer = new("Hash");
    
    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlMap root = merged.GetMap();
        BymlMap baseData = root["Data"].GetMap();
        BymlMergeTracking tracking = new();

        foreach (RentedBuffers<byte>.Entry input in inputs) {
            BymlMap changelog = Byml.FromBinary(input.Span)
                .GetMap();
            MergeEntry(baseData, changelog, tracking);
        }

        tracking.Apply();

        SaveDataWriter.CalculateMetadata(root["MetaData"].GetMap(), baseData);
        merged.WriteBinary(output, endianness, version);
    }

    public void Merge(TkChangelogEntry entry, IEnumerable<ArraySegment<byte>> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlMap root = merged.GetMap();
        BymlMap baseData = root["Data"].GetMap();
        BymlMergeTracking tracking = new();

        foreach (ArraySegment<byte> input in inputs) {
            BymlMap changelog = Byml.FromBinary(input)
                .GetMap();
            MergeEntry(baseData, changelog, tracking);
        }

        tracking.Apply();

        SaveDataWriter.CalculateMetadata(root["MetaData"].GetMap(), baseData);
        merged.WriteBinary(output, endianness, version);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Byml merged = Byml.FromBinary(@base, out Endianness endianness, out ushort version);
        BymlMap root = merged.GetMap();
        BymlMap baseData = root["Data"].GetMap();
        BymlMergeTracking tracking = new();

        BymlMap changelog = Byml.FromBinary(input)
            .GetMap();
        MergeEntry(baseData, changelog, tracking);

        tracking.Apply();

        SaveDataWriter.CalculateMetadata(root["MetaData"].GetMap(), baseData);
        merged.WriteBinary(output, endianness, version);
    }

    private static void MergeEntry(BymlMap merged, BymlMap changelog, BymlMergeTracking tracking)
    {
        foreach ((string key, Byml entry) in changelog) {
            BymlArray @base = merged[key].GetArray();
            switch (entry.Value) {
                case IDictionary<uint, Byml> hashMap32:
                    MergeHashMap32(@base, hashMap32, tracking);
                    break;
                case IDictionary<ulong, Byml> hashMap64:
                    MergeHashMap64(@base, hashMap64, tracking);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Invalid GameDataList changelog array map type: '{entry.Type}'");
            }
            
            @base.Sort(_rowComparer);
        }
    }

    private static void MergeHashMap32(BymlArray @base, IDictionary<uint, Byml> changelog, BymlMergeTracking tracking)
    {
        foreach (Byml baseEntry in @base) {
            if (baseEntry.Value is not IDictionary<string, Byml> map ||
                !map.TryGetValue("Hash", out Byml? hashEntry) || hashEntry.Value is not uint hash) {
                // Invalid vanilla GDL entry
                continue;
            }

            if (!changelog.Remove(hash, out Byml? changelogEntry)) {
                continue;
            }

            BymlMerger.Merge(baseEntry, changelogEntry, tracking);
        }

        foreach ((uint _, Byml entry) in changelog) {
            @base.Add(entry);
        }
    }

    private static void MergeHashMap64(BymlArray @base, IDictionary<ulong, Byml> changelog, BymlMergeTracking tracking)
    {
        foreach (Byml baseEntry in @base) {
            if (baseEntry.Value is not IDictionary<string, Byml> map ||
                !map.TryGetValue("Hash", out Byml? hashEntry) || hashEntry.Value is not ulong hash) {
                // Invalid vanilla GDL entry
                continue;
            }

            if (!changelog.Remove(hash, out Byml? changelogEntry)) {
                continue;
            }

            BymlMerger.Merge(baseEntry, changelogEntry, tracking);
        }

        foreach ((ulong _, Byml entry) in changelog) {
            @base.Add(entry);
        }
    }
}