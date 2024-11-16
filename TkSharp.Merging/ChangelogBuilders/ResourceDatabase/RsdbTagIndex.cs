using System.Buffers;
using System.Collections.Frozen;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;

namespace TkSharp.Merging.ChangelogBuilders.ResourceDatabase;

public sealed class RsdbTagIndex : IDisposable
{
    public readonly FrozenDictionary<(string, string, string), int> Entries;
    public readonly FrozenSet<string> Tags;
    public readonly FrozenSet<string>[] EntryTags;

    public RsdbTagIndex(Span<byte> src) : this(Byml.FromBinary(src).GetMap())
    {
    }

    public RsdbTagIndex(BymlMap table)
    {
        BymlArray paths = table[RsdbTagTable.PATH_LIST].GetArray();
        BymlArray tags = table[RsdbTagTable.TAG_LIST].GetArray();
        int entryCount = paths.Count / 3;

        byte[] bitTable = table[RsdbTagTable.BIT_TABLE].GetBinary();

        Dictionary<(string, string, string), int> entries = new(entryCount);
        EntryTags = ArrayPool<FrozenSet<string>>.Shared.Rent(entryCount);

        for (int i = 0; i < paths.Count; i++) {
            int entryIndex = i / 3;
            entries.Add(
                (paths[i].GetString(), paths[++i].GetString(), paths[++i].GetString()), entryIndex
            );

            EntryTags[entryIndex] = GetEntryTags(entryIndex, tags, bitTable);
        }

        Entries = entries.ToFrozenDictionary();

        Tags = new HashSet<string>(tags.Select(x => x.GetString()), StringComparer.Ordinal)
            .ToFrozenSet();
    }

    public bool HasEntry(BymlArray entries, ref int prefixIndex, out int index, out (Byml Prefix, Byml Name, Byml Suffix) entry)
    {
        return HasEntry(entry = (
            Prefix: entries[prefixIndex],
            Name: entries[++prefixIndex],
            Suffix: entries[++prefixIndex]
        ), out index);
    }

    public bool HasEntry((Byml Prefix, Byml Name, Byml Suffix) entry, out int index)
    {
        index = entry switch {
            { Prefix.Type: BymlNodeType.String, Name.Type: BymlNodeType.String, Suffix.Type: BymlNodeType.String }
                => Entries.GetValueOrDefault((entry.Prefix.GetString(), entry.Name.GetString(), entry.Suffix.GetString()), defaultValue: -1),
            _ => -1
        };

        return index != -1;
    }

    public bool HasTag(Byml tag)
    {
        return tag.Type == BymlNodeType.String && Tags.Contains(tag.GetString());
    }

    private static FrozenSet<string> GetEntryTags(int entryIndex, BymlArray tags, Span<byte> bitTable)
    {
        return RsdbTagTable.GetEntryTags<HashSet<string>>(entryIndex, tags, bitTable).ToFrozenSet();
    }

    public void Dispose()
    {
        ArrayPool<FrozenSet<string>>.Shared.Return(EntryTags);
    }
}