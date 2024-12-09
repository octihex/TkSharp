using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.Models;
using TkSharp.Packaging;

namespace TkSharp.DevTools.ViewModels;

public partial class PackagingPageViewModel : ObservableObject
{
    public PackagingPageViewModel()
    {
        TkProjectManager.Load();
    }
    
    [ObservableProperty]
    private TkProject? _project;
    
    [RelayCommand]
    private async Task NewProject()
    {
        if (await App.Storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Create a TotK mod project folder." }) is not [IStorageFolder folder]) {
            TkLog.Instance.LogInformation("Folder picker operation returned an invalid result or was cancelled.");
            return;
        }

        if (folder.TryGetLocalPath() is not string localFolderPath) {
            TkLog.Instance.LogError(
                "Storage folder {Folder} could not be converted into a local folder path.",
                folder);
            return;
        }
        
        Project = TkProjectManager.NewProject(localFolderPath);
        TkProjectManager.Save();
    }
    
    [RelayCommand]
    private async Task OpenProject()
    {
        FilePickerOpenOptions filePickerOpenOptions = new() {
            Title = "Open a TotK mod project.",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("TotK Project") {
                    Patterns = [
                        "*.tkproj"
                    ]
                }
            ]
        };
        
        if (await App.Storage.OpenFilePickerAsync(filePickerOpenOptions) is not [IStorageFile file]) {
            TkLog.Instance.LogInformation("File picker operation returned an invalid result or was cancelled.");
            return;
        }

        if (file.TryGetLocalPath() is not string localFilePath || Path.GetDirectoryName(localFilePath) is not string localFolderPath) {
            TkLog.Instance.LogError(
                "Storage file {File} could not be converted into a local file path.",
                file);
            return;
        }
        
        Project = TkProjectManager.OpenProject(localFolderPath);
        TkProjectManager.Save();
    }
    
    [RelayCommand]
    private void Save()
    {
        Project?.Save();
        Project = null;
        TkProjectManager.Save();
    }
    
    [RelayCommand]
    private async Task Package()
    {
        if (Project is null) {
            return;
        }
        
        FilePickerSaveOptions filePickerOptions = new() {
            Title = "Export TotK changelog package.",
            SuggestedFileName = $"{Project.Mod.Name}.tkcl",
            DefaultExtension = ".tkcl",
            FileTypeChoices = [
                new FilePickerFileType("TotK Changelog Package") {
                    Patterns = [
                        "*.tkcl"
                    ]
                }
            ]
        };
        
        if (await App.Storage.SaveFilePickerAsync(filePickerOptions) is not IStorageFile file) {
            TkLog.Instance.LogInformation("File picker operation returned an invalid result or was cancelled.");
            return;
        }

        await using Stream output = await file.OpenWriteAsync();
        await Project.Package(output, TkApp.TkRomProvider.GetRom());
    }
    
    [RelayCommand]
    private async Task Install()
    {
        if (Project is null) {
            return;
        }

        ITkModWriter writer = TkApp.ModManager.GetSystemWriter(new TkModContext(Project.Mod.Id));
        await Project.Build(writer,
            TkApp.TkRomProvider.GetRom(),
            TkApp.ModManager.GetSystemSource(Project.Mod.Id.ToString())
        );
    }
}