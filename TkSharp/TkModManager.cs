using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TkSharp.Core;
using TkSharp.Core.IO.ModSources;
using TkSharp.Core.Models;
using TkSharp.IO;
using TkSharp.IO.Serialization;

namespace TkSharp;

public sealed partial class TkModManager(string dataFolderPath) : ObservableObject, ITkSystemProvider
{
    public static TkModManager CreatePortable()
    {
        string portableDataFolder = Path.Combine(AppContext.BaseDirectory, ".data");
        return Create(portableDataFolder);
    }

    public static TkModManager Create(string dataFolderPath)
    {
        string portableManagerStateFile = Path.Combine(dataFolderPath, "state.db");
        if (!File.Exists(portableManagerStateFile)) {
            return new TkModManager(dataFolderPath);
        }
        
        using FileStream fs = File.OpenRead(portableManagerStateFile);
        return TkBinaryReader.Read(fs, dataFolderPath);
    }

    public string DataFolderPath { get; } = dataFolderPath;

    public string ModsFolderPath { get; } = Path.Combine(dataFolderPath, "contents");

    [ObservableProperty]
    private TkMod? _selected;

    [ObservableProperty]
    private TkProfile? _currentProfile;

    public ObservableCollection<TkMod> Mods { get; } = [];

    public ObservableCollection<TkProfile> Profiles { get; } = [];

    public TkProfile GetCurrentProfile()
    {
        EnsureProfiles();
        
        return CurrentProfile ?? Profiles[0];
    }

    public ITkModWriter GetSystemWriter(TkModContext modContext)
    {
        return new SystemModWriter(this, modContext.Id);
    }

    public ITkSystemSource GetSystemSource(string relativeFolderPath)
    {
        return new TkSystemSource(
            Path.Combine(ModsFolderPath, relativeFolderPath));
    }

    public void Add(TkMod target, TkProfile? profile = null)
    {
        EnsureProfiles();

        profile ??= CurrentProfile ?? Profiles[0];

        Mods.Add(target);
        profile.Mods.Add(new TkProfileMod(target));
    }

    public void Save()
    {
        Directory.CreateDirectory(DataFolderPath);

        using FileStream fs = File.Create(Path.Combine(DataFolderPath, "state.db"));
        TkBinaryWriter.Write(fs, this);
    }

    private void EnsureProfiles()
    {
        if (Profiles.Count > 0) {
            return;
        }

        Profiles.Add(new TkProfile {
            Name = "Default"
        });
    }
}