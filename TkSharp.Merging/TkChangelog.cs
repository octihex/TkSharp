using System.Text.Json.Serialization;
using TkSharp.Core;

namespace TkSharp.Merging;

public class TkChangelog
{
    public required int BuilderVersion { get; init; }

    public required int GameVersion { get; init; }

    public List<TkChangelogEntry> ChangelogFiles { get; } = [];

    public List<string> MalsFiles { get; } = [];

    public List<TkPatch> PatchFiles { get; } = [];

    public List<string> SubSdkFiles { get; } = [];

    public List<string> CheatFiles { get; } = [];

    public TkChangelog()
    {
    }

    [JsonConstructor]
    private TkChangelog(int builderVersion, int gameVersion, List<TkChangelogEntry> changelogFiles,
        List<string> malsFiles, List<TkPatch> patchFiles, List<string> subSdkFiles, List<string> cheatFiles)
    {
        BuilderVersion = builderVersion;
        GameVersion = gameVersion;
        ChangelogFiles = changelogFiles;
        MalsFiles = malsFiles;
        PatchFiles = patchFiles;
        SubSdkFiles = subSdkFiles;
        CheatFiles = cheatFiles;
    }
}