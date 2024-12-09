using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;

namespace TkSharp.DevTools.Views;

public partial class MergingPage : UserControl
{
    public MergingPage()
    {
        InitializeComponent();
        DropTarget.AddHandler(DragDrop.DropEvent, OnDragDrop);
    }
    
    private static async void OnDragDrop(object? sender, DragEventArgs e)
    {
        try {
            if (e.Data.GetFiles() is not IEnumerable<IStorageItem> targets) {
                return;
            }

            foreach (IStorageItem item in targets) {
                if (item.TryGetLocalPath() is not string path) {
                    TkLog.Instance.LogError("Failed to read storage item '{Item}'", item);
                    continue;
                }

                if (await TkApp.ReaderProvider.ReadFromInput(path) is not TkMod mod) {
                    TkLog.Instance.LogError("Failed parse local file/folder '{LocalPath}'", path);
                    continue;
                }
                
                TkLog.Instance.LogInformation("Installing local file/folder '{LocalPath}'", path);
                TkApp.ModManager.Import(mod);
                TkApp.ModManager.Save();
            }
        }
        catch (Exception ex) {
            TkLog.Instance.LogError(ex, "Error processing drag/drop event.");
        }
    }
}