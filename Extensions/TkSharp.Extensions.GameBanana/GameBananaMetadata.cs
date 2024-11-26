using System.Text.Json.Serialization;

namespace TkSharp.Extensions.GameBanana;

public class GameBananaMetadata
{
    [JsonPropertyName("_bIsComplete")]
    public bool IsCompleted { get; set; }
}
