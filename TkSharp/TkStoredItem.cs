using TkSharp.Merging;

namespace TkSharp;

public abstract class TkStoredItem : TkItem
{
    public Ulid Id { get; init; }

    public TkChangelog Changelog { get; init; } = null!;
}