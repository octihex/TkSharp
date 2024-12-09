using System.Collections.ObjectModel;
using System.Text.Json;
using TkSharp.Core.Models;

namespace TkSharp.Packaging;

public static class TkProjectManager
{
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

    private static void LoadProjectMetadataFromFolder(TkProject project)
    {
        string projectFilePath = Path.Combine(project.FolderPath, ".tkproj");
        if (!File.Exists(projectFilePath)) {
            project.Mod = new TkMod {
                Name = Path.GetFileNameWithoutExtension(projectFilePath)
            };
            
            LoadProjectOptionsFromFolder(project);
            return;
        }

        using FileStream fs = File.OpenRead(projectFilePath);
        project.Mod = JsonSerializer.Deserialize<TkMod>(fs)
                      ?? new TkMod();

        LoadProjectOptionsFromFolder(project);
    }

    private static void LoadProjectOptionsFromFolder(TkProject project)
    {
        
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
            RecentProjects.Insert(i, project);
        }
        
    Default:
        if (RecentProjects.Count > MaxRecentProjects) {
            RecentProjects.RemoveAt(RecentProjects.Count - 1);
        }
        
        RecentProjects.Insert(0, project);
    }
}