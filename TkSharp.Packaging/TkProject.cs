using System.Diagnostics.CodeAnalysis;
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
    
    private readonly Dictionary<TkItem, string> _itemPathLookup = [];

    public async ValueTask Package(Stream output, ITkRom rom, CancellationToken ct = default)
    {
        TkLog.Instance.LogInformation("Packaging '{ModName}'", Mod.Name);
        
        ArchiveModWriter writer = new();
        await Build(writer, rom, ct: ct);
        
        using MemoryStream contentArchiveOutput = new();
        writer.Compile(contentArchiveOutput);

        TkPackWriter.Write(output, Mod, contentArchiveOutput.GetSpan());
        
        TkLog.Instance.LogInformation("'{ModName}' packaging completed", Mod.Name);
    }

    public async ValueTask Build(ITkModWriter writer, ITkRom rom, ITkSystemSource? systemSource = null, CancellationToken ct = default)
    {
        FolderModSource source = new(FolderPath);
        Mod.Changelog = await Build(Mod, source, writer, rom, systemSource, ct);

        foreach (TkModOption option in Mod.OptionGroups.SelectMany(group => group.Options)) {
            if (!TryGetPath(option, out string? optionPath)) {
                continue;
            }
            
            FolderModSource optionSource = new(optionPath);
            writer.SetRelativeFolder(option.Id.ToString());
            option.Changelog = await Build(option, optionSource, writer, rom, ct: ct);
        }
    }

    private static async ValueTask<TkChangelog> Build(TkStoredItem item, ITkModSource source, ITkModWriter writer, ITkRom rom, ITkSystemSource? systemSource = null, CancellationToken ct = default)
    {
        TkLog.Instance.LogInformation("Building '{ItemName}'", item.Name);
        
        TkChangelogBuilder builder = new(source, writer, rom, systemSource);
        TkChangelog result = await builder.BuildAsync(ct)
            .ConfigureAwait(false);
        
        TkLog.Instance.LogInformation("'{ItemName}' build completed", item.Name);
        return result;
    }

    public void Save()
    {
        Directory.CreateDirectory(FolderPath);
        string projectFilePath = Path.Combine(FolderPath, ".tkproj");
        using FileStream output = File.Create(projectFilePath);
        JsonSerializer.Serialize(output, Mod);
        
        SaveOptionsGroups();
    }

    private void SaveOptionsGroups()
    {
        foreach (TkModOptionGroup group in Mod.OptionGroups) {
            if (!TryGetPath(group, out string? groupFolderPath)) {
                continue;
            }
            
            string metadataFilePath = Path.Combine(groupFolderPath, "info.json");
            using FileStream fs = File.Create(metadataFilePath);
            JsonSerializer.Serialize(fs, group);

            SaveOptions(group);
        }
    }

    private void SaveOptions(TkModOptionGroup group)
    {
        foreach (TkModOption option in group.Options) {
            if (!TryGetPath(option, out string? optionPath)) {
                continue;
            }
            
            string metadataFilePath = Path.Combine(optionPath, "info.json");
            using FileStream fs = File.Create(metadataFilePath);
            JsonSerializer.Serialize(fs, group);
        }
    }

    public void RegisterItem(TkItem option, string sourceFolderPath)
    {
        _itemPathLookup[option] = sourceFolderPath;
    }

    private bool TryGetPath(TkItem item, [MaybeNullWhen(false)] out string path)
    {
        if (_itemPathLookup.TryGetValue(item, out string? folderPath) && Directory.Exists(folderPath)) {
            path = folderPath;
            return true;
        }
        
        TkLog.Instance.LogWarning("""
            The source folder for the item '{ItemName}' could not be found.
            The folder may have been moved or deleted, this item will not be part of the output.
            """, item.Name);

        path = null;
        return false;
    }
}