using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using TkSharp.Core;
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

    public async ValueTask Package(Stream output, ITkRom rom)
    {
        using MemoryStream contentArchiveOutput = new();
        ArchiveModWriter writer = new(contentArchiveOutput);
        await Build(writer, rom);
        
        TkPackWriter.Write(output, Mod, contentArchiveOutput.GetBuffer());
    }

    public async ValueTask<TkChangelog> Build(ITkModWriter writer, ITkRom rom, ITkSystemSource? systemSource = null)
    {
        FolderModSource source = new(FolderPath);
        TkChangelogBuilder builder = new(source, writer, rom, systemSource);
        return await builder.BuildAsync()
            .ConfigureAwait(false);
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