namespace TkSharp.Core.Models;

public abstract class TkStoredItem : TkItem
{
    public Ulid Id { get; init; } = Ulid.NewUlid();

    public TkChangelog Changelog { get; set; } = null!;
}