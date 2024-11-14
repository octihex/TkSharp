using TkSharp.Core;

namespace TkSharp.Merging;

public record TkChangelogEntry(ChangelogEntryType Type, TkFileAttributes Attributes, int ZsDictionaryId);

public enum ChangelogEntryType
{
    Changelog,
    Copy
}