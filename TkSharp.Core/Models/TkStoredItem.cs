namespace TkSharp.Core.Models;

public abstract class TkStoredItem : TkItem
{
    public Ulid Id { get; init; }

    public TkChangelog Changelog { get; init; } = null!;
}