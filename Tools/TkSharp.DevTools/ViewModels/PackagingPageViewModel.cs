using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Packaging;

namespace TkSharp.DevTools.ViewModels;

public partial class PackagingPageViewModel : ObservableObject
{
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
    }
    
    [RelayCommand]
    private async Task OpenProject()
    {
        if (await App.Storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Open a TotK mod project folder." }) is not [IStorageFolder folder]) {
            TkLog.Instance.LogInformation("Folder picker operation returned an invalid result or was cancelled.");
            return;
        }

        if (folder.TryGetLocalPath() is not string localFolderPath) {
            TkLog.Instance.LogError(
                "Storage folder {Folder} could not be converted into a local folder path.",
                folder);
            return;
        }
        
        Project = TkProjectManager.OpenProject(localFolderPath);
    }
    
    [RelayCommand]
    private static async Task Save()
    {
        
    }
    
    [RelayCommand]
    private static async Task Package()
    {
        
    }
    
    [RelayCommand]
    private static async Task PackageAndInstall()
    {
        
    }
}