using System.Text.Json.Serialization;

namespace TkSharp.Extensions.GameBanana;

public class GameBananaImage
{
    [JsonPropertyName("_sBaseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("_sFile")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("_sFile530")]
    public string SmallFile { get; set; } = string.Empty;
}