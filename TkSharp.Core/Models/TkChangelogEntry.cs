using System.Text.Json.Serialization;

namespace TkSharp.Core.Models;

[method: JsonConstructor]
public class TkChangelogEntry(string canonical, ChangelogEntryType type, TkFileAttributes attributes, int zsDictionaryId)
{
    public string Canonical { get; set; } = canonical;

    public ChangelogEntryType Type { get; init; } = type;

    public TkFileAttributes Attributes { get; init; } = attributes;

    public int ZsDictionaryId { get; init; } = zsDictionaryId;

    public void Deconstruct(out string canonical, out ChangelogEntryType type, out TkFileAttributes attributes, out int zsDictionaryId)
    {
        canonical = Canonical;
        type = Type;
        attributes = Attributes;
        zsDictionaryId = ZsDictionaryId;
    }
}

public enum ChangelogEntryType
{
    Changelog,
    Copy
}