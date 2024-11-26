using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace TkSharp.Extensions.GameBanana;

public class GameBananaCreditGroup
{
    [JsonPropertyName("_sGroupName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_aAuthors")]
    public ImmutableList<GameBananaAuthor> Authors { get; set; } = [];
}