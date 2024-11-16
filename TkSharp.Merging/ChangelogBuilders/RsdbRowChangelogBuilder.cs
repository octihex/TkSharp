using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using CommunityToolkit.HighPerformance;
using Revrs;
using TkSharp.Core;
using TkSharp.Merging.ChangelogBuilders.BinaryYaml;
using TkSharp.Merging.ChangelogBuilders.ResourceDatabase;

namespace TkSharp.Merging.ChangelogBuilders;

public static class RsdbRowChangelogBuilder
{
    public static readonly RsdbRowChangelogBuilder<string> RowId = new("__RowId");
    public static readonly RsdbRowChangelogBuilder<string> Name = new("Name");
    public static readonly RsdbRowChangelogBuilder<string> FullTagId = new("FullTagId");
    public static readonly RsdbRowChangelogBuilder<uint> NameHash = new("NameHash");
}

public sealed class RsdbRowChangelogBuilder<TKey>(string keyName) : ITkChangelogBuilder where TKey : notnull
{
    private readonly string _keyName = keyName;

    public void Build(string canonical, in TkPath path, ArraySegment<byte> srcBuffer, ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite)
    {
        ulong dbNameHash = GetDbNameHash(path);

        Dictionary<TKey, Byml> changelog = [];
        Byml root = Byml.FromBinary(srcBuffer, out Endianness endianness, out ushort version);
        BymlArray rows = root.GetArray();

        BymlArray? vanillaRows = null;

        BymlTrackingInfo bymlTrackingInfo = new(path.Canonical, level: 0);

        foreach (Byml row in rows) {
            Byml rowEntry = row;
            BymlMap entry = rowEntry.GetMap();

            if (!TryGetKeyHash(entry, out ulong keyHash, out TKey? key)) {
                // TODO: Log missing key entry
                continue;
            }
            
            if (!RsdbIndex.TryGetIndex(dbNameHash, keyHash, out int index)) {
                goto UpdateChangelog;
            }

            if (!RsdbVersionCache.TryGetVanilla(dbNameHash, keyHash, path.FileVersion, out Byml? vanillaRow)) {
                vanillaRows ??= Byml.FromBinary(vanillaBuffer).GetArray();
                vanillaRow = vanillaRows[index];
            }
            
            if (BymlChangelogBuilder.LogChangesInline(ref bymlTrackingInfo, ref rowEntry, vanillaRow)) {
                continue;
            }
            
        UpdateChangelog:
            changelog[key] = rowEntry;
        }

        if (changelog.Count is 0) {
            return;
        }

        using MemoryStream ms = new();
        Byml changelogByml = changelog switch {
            IDictionary<uint, Byml> hashMap32 => new Byml(hashMap32),
            IDictionary<string, Byml> map => new Byml(map),
            _ => throw new NotSupportedException(
                $"The type '{typeof(TKey)}' is not a supported RSDB map type.")
        };
        
        changelogByml.WriteBinary(ms, endianness, version);
        ms.Seek(0, SeekOrigin.Begin);
        
        using Stream output = openWrite(path, canonical);
        ms.CopyTo(output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetKeyHash(in BymlMap row, out ulong keyHash, [MaybeNullWhen(false)] out TKey key)
    {
        if (!row.TryGetValue(_keyName, out Byml? keyEntry)) {
            keyHash = default;
            key = default;
            return false;
        }

        keyHash = keyEntry.Value switch {
            string stringKey => XxHash3.HashToUInt64(stringKey.AsSpan().Cast<char, byte>()),
            uint uInt32 => uInt32,
            _ => throw new NotSupportedException(
                $"The BYML type '{keyEntry.Type}' is not a supported RSDB key type."),
        };
        
        key = keyEntry.Get<TKey>();
        
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetDbNameHash(in TkPath path)
    {
        return XxHash3.HashToUInt64(MemoryMarshal.Cast<char, byte>(path.Canonical));
    }
}