using System.Text.Json.Serialization;

namespace TkSharp.Extensions.GameBanana;

public class GameBananaGame
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }
}