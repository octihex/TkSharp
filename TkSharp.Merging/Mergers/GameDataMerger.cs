using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Revrs;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.Mergers.BinaryYaml;
using TkSharp.Merging.Mergers.GameData;

namespace TkSharp.Merging.Mergers;

public sealed class GameDataMerger(TkZstd zs) : ITkMerger
{
    private readonly TkZstd _zs = zs;

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

        GenerateMetadata(root["MetaData"].GetMap(), baseData);

        BymlMerger.WriteOutput(entry, merged, endianness, version, output, _zs);
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

        GenerateMetadata(root["MetaData"].GetMap(), baseData);

        BymlMerger.WriteOutput(entry, merged, endianness, version, output, _zs);
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

        GenerateMetadata(root["MetaData"].GetMap(), baseData);

        BymlMerger.WriteOutput(entry, merged, endianness, version, output, _zs);
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

    private static void GenerateMetadata(BymlMap metadata, BymlMap tables)
    {
        BymlArray saveDataOffsets = new(7);
        BymlArray saveDataSizes = new(7);

        BymlArray saveDirectories = metadata["SaveDirectory"]
            .GetArray();

        for (int i = 0; i < 7; i++) {
            (int offset, int size) = GetSaveDataOffset(i, saveDirectories, tables);
            saveDataOffsets.Add(offset);
            saveDataSizes.Add(size);
        }

        int allDataSaveOffset = 0;
        int allDataSaveSize = 0;

        // foreach ((string tableNameStr, Byml table) in tables) {
        //     BymlArray tableEntries = table.GetArray();
        //     ReadOnlySpan<char> tableName = tableNameStr.AsSpan();
        //
        //     switch (tableName) {
        //         case "Struct" or "BoolExp":
        //             continue;
        //         case "Bool64bitKey":
        //             allDataSaveOffset += 8;
        //             allDataSaveSize += 8;
        //             CalculateBool64Table(ref allDataSaveOffset, ref allDataSaveSize, -1, tableEntries);
        //             continue;
        //     }
        //
        //     if (tableName.Length > 5 && tableName[^5..] is "Array") {
        //         CalculateArrayTable(ref allDataSaveOffset, ref allDataSaveSize, -1, tableName, tableEntries);
        //         continue;
        //     }
        //     
        //     CalculateTable(ref allDataSaveOffset, ref allDataSaveSize, -1, tableName, tableEntries);
        // }

        metadata["AllDataSaveOffset"] = allDataSaveOffset;
        metadata["AllDataSaveSize"] = allDataSaveSize;
        metadata["SaveDataOffsetPos"] = saveDataOffsets;
        metadata["SaveDataSize"] = saveDataSizes;
    }

    private static (int Offset, int Size) GetSaveDataOffset(int index, in BymlArray saveDirectories, in BymlMap tables)
    {
        if (index != -1 && (saveDirectories[index].Value is not string saveDirectory || string.IsNullOrWhiteSpace(saveDirectory))) {
            return (Offset: 0, Size: 0);
        }

        int offset = 0x20;
        int size = 0x20;

        foreach (string tableNameStr in SaveDataWriter.ValidTables) {
            ReadOnlySpan<char> tableName = tableNameStr.AsSpan();

            offset += 8;
            size += 8;

            if (tableName is "Bool64bitKey") {
                offset += 8;
                size += 8;
                CalculateBool64Table(ref size, index, tables[tableNameStr].GetArray());
                continue;
            }

            if (!tables.TryGetValue(tableNameStr, out Byml? tableEntry) || tableEntry.Value is not BymlArray tableEntries) {
                continue;
            }

            if (tableName.Length > 5 && tableName[^5..] is "Array") {
                CalculateArrayTable(ref offset, ref size, index, tableName, tableEntries);
                continue;
            }

            CalculateTable(ref offset, ref size, index, tableName, tableEntries);
        }

        return (offset, size);
    }

    private static void CalculateBool64Table(ref int size, int index, BymlArray table)
    {
        bool hasKeys = false;

        foreach (Byml entry in table) {
            if (!IsMatchingSaveFileIndex(entry, index, out IDictionary<string, Byml>? entryMap)) {
                continue;
            }

            hasKeys = true;
            size += SaveDataGetSize("Bool64bitKey", entryMap);
        }

        if (hasKeys) {
            size += 8;
        }
    }

    private static void CalculateArrayTable(ref int offset, ref int size, int index, ReadOnlySpan<char> tableName, BymlArray table)
    {
        foreach (Byml entry in table) {
            if (!IsMatchingSaveFileIndex(entry, index, out IDictionary<string, Byml>? entryMap)) {
                continue;
            }

            offset += 8;
            size += SaveDataGetArraySize(tableName, entryMap);
        }
    }

    private static void CalculateTable(ref int offset, ref int size, int index, ReadOnlySpan<char> tableName, BymlArray table)
    {
        foreach (Byml entry in table) {
            if (!IsMatchingSaveFileIndex(entry, index, out IDictionary<string, Byml>? entryMap)) {
                continue;
            }

            offset += 8;
            size += SaveDataGetSize(tableName, entryMap);
        }
    }

    private static int SaveDataGetArraySize(ReadOnlySpan<char> tableName, IDictionary<string, Byml> entry)
    {
        int count;

        if (entry.TryGetValue("ArraySize", out Byml? entryArraySize)) {
            count = (int)entryArraySize.GetUInt32();
            goto CalculateSize;
        }

        if (entry.TryGetValue("Size", out Byml? entrySize)) {
            count = (int)entrySize.GetUInt32();
            goto CalculateSize;
        }

        if (entry.TryGetValue("DefaultValue", out Byml? defaultValue) && defaultValue.Value is BymlArray defaultValueArray) {
            count = defaultValueArray.Count;
            goto CalculateSize;
        }

        throw new InvalidDataException(
            $"The length of an array entry in '{tableName}' could not be determined.");

    CalculateSize:
        return SaveDataGetSize(
            size: 0xC, count, tableName, tableName[..^5], entry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SaveDataGetSize(ReadOnlySpan<char> tableName, IDictionary<string, Byml> entry)
    {
        return SaveDataGetSize(
            size: 0x8, count: 1, tableName, tableName, entry);
    }

    private static int SaveDataGetSize(int size, int count, ReadOnlySpan<char> tableName, ReadOnlySpan<char> tableType, IDictionary<string, Byml> entry)
    {
        switch (tableName) {
            case "BoolArray": {
                double div = Math.Ceiling(count / 8d);
                return size + (int)Math.Ceiling((div < 4 ? 4 : div) / 4) * 4;
            }
            case "IntArray" or "FloatArray" or "UIntArray" or "EnumArray":
                return size + count * 4;
            case "Binary" or "BinaryArray": {
                if (entry.TryGetValue("DefaultValue", out Byml? entryDefaultValue) && entryDefaultValue.Value is uint defaultValue) {
                    return size + count * 4
                                + count * (int)defaultValue;
                }

                return count * 4;
            }
        }

        return size + count * tableType switch {
            "Int64" or "Vector2" => 8,
            "Vector3" => 12,
            "String16" => 16,
            "String32" or "WString16" => 32,
            "String64" or "WString32" => 64,
            "WString64" => 128,
            _ => 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatchingSaveFileIndex(Byml entry, int index, [MaybeNullWhen(false)] out IDictionary<string, Byml> entryMap)
    {
        entryMap = entry.Value as IDictionary<string, Byml>;
        return entryMap is not null
               && entryMap.TryGetValue("SaveFileIndex", out Byml? entrySaveFileIndex)
               && entrySaveFileIndex.Value is int saveFileIndex
               && saveFileIndex == index;
    }
}