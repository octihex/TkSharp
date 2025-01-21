using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;
using TkSharp.IO;
using TkSharp.IO.Serialization;
using TkSharp.IO.Writers;

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
        return TkModManagerSerializer.Read(fs, dataFolderPath);
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

    public IEnumerable<TkChangelog> GetMergeTargets() => GetMergeTargets(GetCurrentProfile());

    public static IEnumerable<TkChangelog> GetMergeTargets(TkProfile profile)
    {
        foreach ((TkModOptionGroup group, HashSet<TkModOption> options) in profile.Mods.SelectMany(x => x.SelectedOptions)) {
            if (group.Type is not (OptionGroupType.MultiRequired or OptionGroupType.SingleRequired)) {
                continue;
            }
            
            if (options.Count > 0 || group.Options.Count == 0) {
                continue;
            }
            
            options.Add(group.Options[0]);
        }
        
        return profile
            .Mods
            .Where(x => x.IsEnabled)
            .SelectMany(x => x.SelectedOptions.Values
                .SelectMany(group => group)
                .OrderBy(option => option.Priority)
                .Select(option => option.Changelog)
                .Prepend(x.Mod.Changelog)
            )
            .Reverse();
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

    public void Import(TkMod target, TkProfile? profile = null)
    {
        AddOrUpdate(target);

        profile ??= GetCurrentProfile();
        profile.AddOrUpdate(target);
    }

    public void Uninstall(TkMod target)
    {
        string targetModFolder = Path.Combine(ModsFolderPath, target.Id.ToString());
        if (!Directory.Exists(targetModFolder)) {
            TkLog.Instance.LogDebug("Content for the mod '{TargetName}' could not be found in the system.",
                target.Name);
            goto Remove;
        }

        try {
            Directory.Delete(targetModFolder, true);
        }
        catch (Exception ex) {
            TkLog.Instance.LogError(ex,
                "Failed to delete content for the mod '{TargetName}'. Consider manually deleting the folder '{TargetModFolder}' and then uninstalling this mod again.",
                target.Name, targetModFolder);
            return;
        }

    Remove:
        Mods.Remove(target);

        foreach (TkProfile profile in Profiles) {
            if (profile.Mods.FirstOrDefault(x => x.Mod == target) is TkProfileMod profileMod) {
                profile.Mods.Remove(profileMod);
            }
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DataFolderPath);

        try {
            using MemoryStream ms = new();
            TkModManagerSerializer.Write(ms, this);

            using FileStream fs = File.Create(Path.Combine(DataFolderPath, "state.db"));
            fs.Write(ms.GetSpan());
        }
        catch (Exception ex) {
            TkLog.Instance.LogError(ex, "Failed to save mod manager state.");
#if DEBUG
            throw;
#endif
        }
    }

    /// <summary>
    /// Ensure that the mod being imported is the only instance of it.
    /// </summary>
    /// <param name="target"></param>
    private void AddOrUpdate(TkMod target)
    {
        for (int i = 0; i < Mods.Count; i++) {
            if (Mods[i].Id == target.Id) {
                Mods[i] = target;
                goto UpdateProfiles;
            }
        }
        
        Mods.Add(target);
        return;
        
    UpdateProfiles:
        foreach (TkProfile profile in Profiles) {
            profile.Update(target);
        }
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

    partial void OnCurrentProfileChanged(TkProfile? value)
    {
        value?.RebaseOptions(value.Selected);
    }
}