using System.Collections.ObjectModel;
using System.Text.Json;
using TkSharp.Core.Models;

namespace TkSharp.Packaging;

public static class TkProjectManager
{
    private static readonly string _storeFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".tk-studio", "recent.json");

    public static ObservableCollection<TkProject> RecentProjects { get; } = [];

    public static int MaxRecentProjects { get; set; } = 15;

    public static TkProject NewProject(string sourceFolderPath)
    {
        TkProject project = OpenProject(sourceFolderPath);
        project.Save();
        return project;
    }

    public static TkProject OpenProject(string sourceFolderPath)
    {
        TkProject project = new(sourceFolderPath);
        LoadProjectMetadataFromFolder(project);
        InsertRecentProject(project);
        return project;
    }

    public static void Load()
    {
        if (!File.Exists(_storeFilePath)) {
            return;
        }

        RecentProjects.Clear();
        using FileStream fs = File.OpenRead(_storeFilePath);
        List<string> recentProjectFolders = JsonSerializer.Deserialize<List<string>>(fs)
                                            ?? [];

        foreach (TkProject project in recentProjectFolders.Select(projectFolder => new TkProject(projectFolder))) {
            LoadProjectMetadataFromFolder(project);
            RecentProjects.Add(project);
        }
    }

    public static void Save()
    {
        if (Path.GetDirectoryName(_storeFilePath) is string folder) {
            Directory.CreateDirectory(folder);
        }
        
        using FileStream fs = File.Create(_storeFilePath);
        JsonSerializer.Serialize(fs, RecentProjects.Select(proj => proj.FolderPath));
    }

    private static void LoadProjectMetadataFromFolder(TkProject project)
    {
        string projectFilePath = Path.Combine(project.FolderPath, ".tkproj");
        FileInfo projectFile = new(projectFilePath);
        
        if (!projectFile.Exists || projectFile.Length == 0) {
            project.Mod = new TkMod {
                Name = Path.GetFileNameWithoutExtension(projectFilePath)
            };

            LoadProjectOptionsFromFolder(project);
            return;
        }

        using FileStream fs = projectFile.OpenRead();
        project.Mod = JsonSerializer.Deserialize<TkMod>(fs)
                      ?? new TkMod();

        LoadProjectOptionsFromFolder(project);
    }

    private static void LoadProjectOptionsFromFolder(TkProject project)
    {
        string optionsFolderPath = Path.Combine(project.FolderPath, "options");
        if (!Directory.Exists(optionsFolderPath)) {
            return;
        }

        foreach (string optionGroupFolderPath in Directory.EnumerateDirectories(optionsFolderPath)) {
            LoadOptionGroupFolder(project, optionGroupFolderPath);
        }
    }

    private static void LoadOptionGroupFolder(TkProject project, string optionGroupFolderPath)
    {
        TkModOptionGroup group;

        string metadataFilePath = Path.Combine(optionGroupFolderPath, "info.json");
        FileInfo metadataFile = new(metadataFilePath);
        
        if (metadataFile is { Exists: true, Length: > 0 }) {
            using FileStream fs = File.OpenRead(metadataFilePath);
            group = JsonSerializer.Deserialize<TkModOptionGroup>(fs)
                    ?? new TkModOptionGroup();
            goto CollectOptions;
        }

        group = new TkModOptionGroup {
            Name = Path.GetFileNameWithoutExtension(optionGroupFolderPath)
        };

    CollectOptions:
        foreach (string optionFolderPath in Directory.EnumerateDirectories(optionGroupFolderPath)) {
            LoadOptionFolder(project, group, optionFolderPath);
        }

        project.RegisterItem(group, optionGroupFolderPath);
        project.Mod.OptionGroups.Add(group);
    }

    private static void LoadOptionFolder(TkProject project, TkModOptionGroup group, string optionFolderPath)
    {
        TkModOption option;

        string metadataFilePath = Path.Combine(optionFolderPath, "info.json");
        FileInfo metadataFile = new(metadataFilePath);
        
        if (metadataFile is { Exists: true, Length: > 0 }) {
            using FileStream fs = File.OpenRead(metadataFilePath);
            option = JsonSerializer.Deserialize<TkModOption>(fs) ?? new TkModOption();
            goto Result;
        }

        option = new TkModOption {
            Name = Path.GetFileNameWithoutExtension(optionFolderPath)
        };

    Result:
        project.RegisterItem(option, optionFolderPath);
        group.Options.Add(option);
    }

    private static void InsertRecentProject(TkProject project)
    {
        if (RecentProjects.Count == 0) {
            goto Default;
        }

        for (int i = 0; i < RecentProjects.Count; i++) {
            if (RecentProjects[i].FolderPath != project.FolderPath) {
                continue;
            }

            RecentProjects.RemoveAt(i);
            goto Default;
        }

    Default:
        if (RecentProjects.Count > MaxRecentProjects) {
            RecentProjects.RemoveAt(RecentProjects.Count - 1);
        }

        RecentProjects.Insert(0, project);
    }
}