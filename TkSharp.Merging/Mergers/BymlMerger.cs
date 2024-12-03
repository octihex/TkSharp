using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance.Buffers;
using Revrs;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.Mergers.BinaryYaml;

namespace TkSharp.Merging.Mergers;

public sealed class BymlMerger(TkZstd zs) : ITkMerger
{
    private readonly TkZstd _zs = zs;

    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlMergeTracking tracking = new();

        foreach (RentedBuffers<byte>.Entry input in inputs) {
            Byml changelog = Byml.FromBinary(input.Span);
            Merge(merged, changelog, tracking);
        }
        
        tracking.Apply();
        
        WriteOutput(entry, merged, endianness, version, output, _zs);
    }

    public void Merge(TkChangelogEntry entry, IEnumerable<ArraySegment<byte>> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        Byml merged = Byml.FromBinary(vanillaData, out Endianness endianness, out ushort version);
        BymlMergeTracking tracking = new();

        foreach (ArraySegment<byte> input in inputs) {
            Byml changelog = Byml.FromBinary(input);
            Merge(merged, changelog, tracking);
        }
        
        tracking.Apply();
        
        WriteOutput(entry, merged, endianness, version, output, _zs);
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Byml merged = Byml.FromBinary(@base, out Endianness endianness, out ushort version);
        Byml changelog = Byml.FromBinary(input);
        BymlMergeTracking tracking = new();

        Merge(merged, changelog, tracking);
        
        tracking.Apply();
        
        WriteOutput(entry, merged, endianness, version, output, _zs);
    }

    public static void WriteOutput(TkChangelogEntry entry, Byml merged, Endianness endianness, ushort version, Stream output, TkZstd zs)
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
        int compressedSize = zs.Compress(buffer, compressed.Span, entry.ZsDictionaryId);
        output.Write(compressed.Span[..compressedSize]);
    }

    public static void Merge(Byml @base, Byml changelog, BymlMergeTracking tracking)
    {
        switch (@base.Value) {
            case IDictionary<string, Byml> map:
                MergeMap(map, changelog.GetMap(), tracking);
                break;
            case IDictionary<uint, Byml> hashMap32:
                MergeMap(hashMap32, changelog.GetHashMap32(), tracking);
                break;
            case IDictionary<ulong, Byml> hashMap64:
                MergeMap(hashMap64, changelog.GetHashMap64(), tracking);
                break;
            case BymlArray array when changelog.Value is BymlArrayChangelog arrayChangelog:
                MergeArray(array, arrayChangelog, tracking);
                break;
            case BymlArray existingCustomArray when changelog.Value is BymlArray customArray:
                existingCustomArray.AddRange(customArray);
                break;
            default:
                throw new NotSupportedException(
                    $"Merging BYML changelog type '{changelog.Type}' with vanilla/base type '{@base.Type}' is not supported.");
        }
    }

    public static void MergeMap<T>(IDictionary<T, Byml> @base, IDictionary<T, Byml> changelog, BymlMergeTracking tracking)
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
                Merge(baseEntry, entry, tracking);
                continue;
            }

            @base[key] = entry;
        }
    }

    public static void MergeArray(BymlArray @base, BymlArrayChangelog changelog, BymlMergeTracking tracking)
    {
        foreach ((int i, (BymlChangeType change, Byml entry)) in changelog) {
            switch (change) {
                case BymlChangeType.Add: {
                    if (!tracking.TryGetValue(@base, out BymlMergeTrackingEntry? trackingEntry)) {
                        tracking[@base] = trackingEntry = new BymlMergeTrackingEntry();
                    }
                    
                    trackingEntry.Additions.Add((int.MaxValue - i, entry));
                    break;
                }
                case BymlChangeType.Remove: {
                    if (!tracking.TryGetValue(@base, out BymlMergeTrackingEntry? trackingEntry)) {
                        tracking[@base] = trackingEntry = new BymlMergeTrackingEntry();
                    }
                    
                    trackingEntry.Removals.Add(i);
                    break;
                }
                case BymlChangeType.Edit: {
                    if (tracking.TryGetValue(@base, out BymlMergeTrackingEntry? trackingEntry)) {
                        trackingEntry.Removals.Remove(i);
                    }
                    
                    if (entry.Value is IBymlNode) {
                        Merge(@base[i], entry, tracking);
                        continue;
                    }

                    @base[i] = entry;
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Invalid array changelog entry type: '{change}'");
            }
        }
    }
}