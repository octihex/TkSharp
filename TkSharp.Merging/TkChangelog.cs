using TkSharp.Core;

namespace TkSharp.Merging;

public class TkChangelog()
{
    public required int BuilderVersion { get; init; }

    public required int GameVersion { get; init; }

    public List<(string, TkChangelogEntry)> ChangelogFiles { get; init; } = [];

    public List<string> MalsFiles { get; init; } = [];

    public List<TkPatch> PatchFiles { get; init; } = [];

    public List<string> SubSdkFiles { get; init; } = [];

    public List<string> CheatFiles { get; init; } = [];
}