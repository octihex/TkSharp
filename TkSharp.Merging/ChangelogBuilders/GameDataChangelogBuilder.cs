using System.IO.Hashing;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using BymlLibrary.Nodes.Containers.HashMap;
using CommunityToolkit.HighPerformance;
using Revrs;
using TkSharp.Core;
using TkSharp.Merging.ChangelogBuilders.BinaryYaml;
using TkSharp.Merging.ChangelogBuilders.GameData;

namespace TkSharp.Merging.ChangelogBuilders;

public sealed class GameDataChangelogBuilder : Singleton<GameDataChangelogBuilder>, ITkChangelogBuilder
{
    public void Build(string canonical, in TkPath path, ArraySegment<byte> srcBuffer, ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite)
    {
        BymlMap changelog = [];
        BymlMap src = Byml.FromBinary(srcBuffer, out Endianness endianness, out ushort version).GetMap()["Data"].GetMap();
        BymlMap vanilla = Byml.FromBinary(vanillaBuffer).GetMap()["Data"].GetMap();

        BymlTrackingInfo bymlTrackingInfo = new(path.Canonical, 0);

        foreach ((string tableName, Byml srcEntry) in src) {
            BymlArray entries = srcEntry.GetArray();
            BymlArray vanillaEntries = vanilla[tableName].GetArray();
            
            if (tableName is "Bool64bitKey") {
                if (LogUInt64Entries(ref bymlTrackingInfo, entries, vanillaEntries) is { Count: > 0 } u64LogResult) {
                    changelog[tableName] = u64LogResult;
                }
                
                continue;
            }
            
            ulong tableNameHash = XxHash3.HashToUInt64(
                tableName.AsSpan().Cast<char, byte>());
            if (LogEntries(ref bymlTrackingInfo, tableNameHash, entries, vanillaEntries) is { Count: > 0 } logResult) {
                changelog[tableName] = logResult;
            }
        }

        if (changelog.Count == 0) {
            return;
        }

        using MemoryStream ms = new();
        ((Byml)changelog).WriteBinary(ms, endianness, version);
        ms.Seek(0, SeekOrigin.Begin);

        using Stream output = openWrite(path, canonical);
        ms.CopyTo(output);
    }

    private static BymlHashMap32 LogEntries(ref BymlTrackingInfo bymlTrackingInfo, ulong tableNameHash, BymlArray src, BymlArray vanilla)
    {
        BymlHashMap32 changelog = [];

        foreach (Byml srcEntry in src) {
            Byml srcEntryVar = srcEntry;
            BymlMap entry = srcEntryVar.GetMap();
            if (!entry.TryGetValue("Hash", out Byml? hashEntry) || hashEntry.Value is not uint hash) {
                continue;
            }

            if (!GameDataIndex.TryGetIndex(tableNameHash, hash, out int index)) {
                goto UpdateChangelog;
            }
            
            if (BymlChangelogBuilder.LogChangesInline(ref bymlTrackingInfo, ref srcEntryVar, vanilla[index])) {
                continue;
            }
            
        UpdateChangelog:
            changelog[hash] = entry;
        }

        return changelog;
    }

    private static BymlHashMap64 LogUInt64Entries(ref BymlTrackingInfo bymlTrackingInfo, BymlArray src, BymlArray vanilla)
    {
        BymlHashMap64 changelog = [];

        foreach (Byml srcEntry in src) {
            Byml srcEntryVar = srcEntry;
            BymlMap entry = srcEntryVar.GetMap();
            if (!entry.TryGetValue("Hash", out Byml? hashEntry) || hashEntry.Value is not ulong hash) {
                continue;
            }

            if (!GameDataIndex.TryGetIndex(hash, out int index)) {
                goto UpdateChangelog;
            }
            
            if (BymlChangelogBuilder.LogChangesInline(ref bymlTrackingInfo, ref srcEntryVar, vanilla[index])) {
                continue;
            }
            
        UpdateChangelog:
            changelog[hash] = entry;
        }

        return changelog;
    }
}