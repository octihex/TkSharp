using System.Text.Json.Serialization;

namespace TkSharp.DevTools.Helpers.Ryujinx;

public record RyujinxConfig(
    [property: JsonPropertyName("game_dirs")] List<string> GameDirs
);