namespace TkSharp.Core.Models;

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