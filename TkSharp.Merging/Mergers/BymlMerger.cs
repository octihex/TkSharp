using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance.Buffers;
using Revrs;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;

namespace TkSharp.Merging.Mergers;

public sealed class BymlMerger(TkZstd zs) : ITkMerger
{
    private readonly TkZstd _zs = zs;

    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);

        foreach (RentedBuffers<byte>.Entry input in inputs) {
            Byml changelog = Byml.FromBinary(input.Span);
            Merge(merged, changelog);
        }
        
        WriteOutput(entry, merged, endianness, version, output);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Byml merged = Byml.FromBinary(@base, out Endianness endianness, out ushort version);
        Byml changelog = Byml.FromBinary(input);

        Merge(merged, changelog);
        
        WriteOutput(entry, merged, endianness, version, output);
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

    public static void Merge(Byml @base, Byml changelog)
    {
        switch (@base.Value) {
            case IDictionary<string, Byml> map:
                MergeMap(map, changelog.GetMap());
                break;
            case IDictionary<uint, Byml> hashMap32:
                MergeMap(hashMap32, changelog.GetHashMap32());
                break;
            case IDictionary<ulong, Byml> hashMap64:
                MergeMap(hashMap64, changelog.GetHashMap64());
                break;
            case BymlArray array when changelog.Value is BymlArrayChangelog arrayChangelog:
                MergeArray(array, arrayChangelog);
                break;
            case BymlArray existingCustomArray when changelog.Value is BymlArray customArray:
                existingCustomArray.AddRange(customArray);
                break;
            default:
                throw new NotSupportedException(
                    $"Merging BYML changelog type '{changelog.Type}' with vanilla/base type '{@base.Type}' is not supported.");
        }
    }

    internal static void MergeMap<T>(IDictionary<T, Byml> @base, IDictionary<T, Byml> changelog)
    {
        foreach ((T key, Byml entry) in changelog) {
            if (entry.Value is BymlChangeType.Remove) {
                @base.Remove(key);
                continue;
            }

            if (!@base.TryGetValue(key, out Byml? baseEntry)) {
                @base[key] = entry;
                continue;
            }

            if (entry.Value is IBymlNode && baseEntry.Value is IBymlNode) {
                Merge(baseEntry, entry);
                continue;
            }

            @base[key] = entry;
        }
    }

    internal static void MergeArray(BymlArray @base, BymlArrayChangelog changelog)
    {
        int indexOffset = 0;
        
        foreach ((int index, (BymlChangeType change, Byml entry)) in changelog) {
            int i = index - indexOffset;
            switch (change) {
                case BymlChangeType.Add:
                    int reverseInsertIndex = int.MaxValue - index + indexOffset;
                    if (reverseInsertIndex < @base.Count) {
                        @base.Insert(reverseInsertIndex, entry);
                        break;
                    }
                    
                    @base.Add(entry);
                    break;
                case BymlChangeType.Remove:
                    if (i >= @base.Count) {
                        continue;
                    }

                    @base.RemoveAt(i);
                    indexOffset++;
                    break;
                case BymlChangeType.Edit:
                    if (entry.Value is IBymlNode) {
                        Merge(@base[i], entry);
                        continue;
                    }

                    @base[i] = entry;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Invalid array changelog entry type: '{change}'");
            }
        }
    }
}