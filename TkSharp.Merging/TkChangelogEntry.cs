using System.Text.Json.Serialization;
using TkSharp.Core;

namespace TkSharp.Merging;

public record TkChangelogEntry(
    string Canonical,
    ChangelogEntryType Type,
    TkFileAttributes Attributes,
    int ZsDictionaryId);

public enum ChangelogEntryType
{
    Changelog,
    Copy
}