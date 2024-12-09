using System.Text.Json.Serialization;

namespace TkSharp.Core.Models;

public abstract class TkStoredItem : TkItem
{
    public Ulid Id { get; init; } = Ulid.NewUlid();

    [JsonIgnore]
    public TkChangelog Changelog { get; set; } = null!;
}