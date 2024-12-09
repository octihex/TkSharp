using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.Extensions;
using TkSharp.Core.IO.ModSources;
using TkSharp.Core.Models;
using TkSharp.Merging;
using TkSharp.Packaging.IO.Serialization;
using TkSharp.Packaging.IO.Writers;

namespace TkSharp.Packaging;

public partial class TkProject(string folderPath) : ObservableObject
{
    [ObservableProperty]
    private string _folderPath = folderPath;
    
    [ObservableProperty]
    private TkMod _mod = new();

    public async ValueTask Package(Stream output, ITkRom rom, CancellationToken ct = default)
    {
        TkLog.Instance.LogInformation("Packaging '{ModName}'", Mod.Name);
        
        ArchiveModWriter writer = new();
        Mod.Changelog = await Build(writer, rom, ct: ct);
        
        using MemoryStream contentArchiveOutput = new();
        writer.Compile(contentArchiveOutput);

        TkPackWriter.Write(output, Mod, contentArchiveOutput.GetSpan());
        
        TkLog.Instance.LogInformation("'{ModName}' packaging completed", Mod.Name);
    }

    public async ValueTask<TkChangelog> Build(ITkModWriter writer, ITkRom rom, ITkSystemSource? systemSource = null, CancellationToken ct = default)
    {
        TkLog.Instance.LogInformation("Building '{ModName}'", Mod.Name);
        
        FolderModSource source = new(FolderPath);
        TkChangelogBuilder builder = new(source, writer, rom, systemSource);
        TkChangelog result = await builder.BuildAsync(ct)
            .ConfigureAwait(false);
        
        TkLog.Instance.LogInformation("'{ModName}' build completed", Mod.Name);
        return result;
    }

    public void Save()
    {
        Directory.CreateDirectory(FolderPath);
        string projectFilePath = Path.Combine(FolderPath, ".tkproj");
        using FileStream output = File.Create(projectFilePath);
        JsonSerializer.Serialize(output, Mod);
        
        // TODO: Save options
    }
}