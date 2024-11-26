using System.Text.Json.Serialization;

namespace TkSharp.Extensions.GameBanana;

public class GameBananaMedia
{
    [JsonPropertyName("_aImages")]
    public List<GameBananaImage> Images { get; set; } = [];
}
