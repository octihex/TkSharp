using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TkSharp.Core;
using TkSharp.Core.Models;
using TkSharp.IO;
using TkSharp.IO.Serialization;

namespace TkSharp;

public sealed partial class TkModManager(string dataFolderPath) : ObservableObject, ITkModWriterProvider
{
    public static readonly TkModManager Portable;

    static TkModManager()
    {
        string portableDataFolder = Path.Combine(AppContext.BaseDirectory, ".data");
        string portableManagerStateFile = Path.Combine(portableDataFolder, "state.db");
        using FileStream fs = File.OpenRead(portableManagerStateFile);
        Portable = TkModManagerReader.Read(fs, portableManagerStateFile);
    }

    public string DataFolderPath { get; } = dataFolderPath;

    public string ModsFolderPath { get; } = Path.Combine(dataFolderPath, "mods");

    [ObservableProperty]
    private TkMod? _selected;

    [ObservableProperty]
    private TkProfile? _currentProfile;

    public ObservableCollection<TkMod> Mods { get; } = [];

    public ObservableCollection<TkProfile> Profiles { get; } = [];

    public ITkModWriter GetSystemWriter(TkModContext modContext)
    {
        return new SystemModWriter(this, modContext.Id);
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
        TkModManagerWriter.Write(fs, this);
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