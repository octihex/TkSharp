namespace TkSharp.Core.Models;

public class TkChangelog
{
    public required int BuilderVersion { get; init; }

    public required int GameVersion { get; init; }

    public List<TkChangelogEntry> ChangelogFiles { get; } = [];

    public List<string> MalsFiles { get; } = [];

    public List<TkPatch> PatchFiles { get; } = [];

    public List<TkCheat> CheatFiles { get; init; } = [];
    
    public List<string> SubSdkFiles { get; } = [];

    public List<string> ExeFiles { get; } = [];

    public List<string> Reserved1 { get; } = [];

    public List<string> Reserved2 { get; } = [];

    public ITkSystemSource? Source { get; init; }
}