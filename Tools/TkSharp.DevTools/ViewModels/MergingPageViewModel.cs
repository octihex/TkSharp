using System.Diagnostics;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Kokuban;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;
using TkSharp.IO.Writers;
using TkSharp.Merging;

namespace TkSharp.DevTools.ViewModels;

public partial class MergingPageViewModel : ObservableObject
{
    [RelayCommand]
    private static async Task Import()
    {
        TextBox input = new() {
            Width = 350,
            Watermark = "File path, folder path, or GameBanana mod URL"
        };

        ContentDialog dialog = new() {
            Title = "Import",
            Content = input,
            PrimaryButtonText = "Import",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        if (await dialog.ShowAsync() is not ContentDialogResult.Primary) {
            Console.WriteLine(Chalk.BrightRed + "Operation cancelled.");
            return;
        }

        if (input.Text is null || await TkApp.ReaderProvider.ReadFromInput(input.Text) is not TkMod mod) {
            Console.WriteLine(Chalk.BrightRed + "Invalid input.");
            return;
        }
        
        TkApp.ModManager.Import(mod);
        TkApp.ModManager.Save();
    }
    
    [RelayCommand]
    private static void Remove()
    {
        if (TkApp.ModManager.Selected is not TkMod target) {
            Console.WriteLine(Chalk.BrightRed + "No selection to uninstall.");
            return;
        }
        
        TkApp.ModManager.Uninstall(target);
        TkApp.ModManager.Save();
    }

    [RelayCommand]
    private static void MoveUp()
    {
        TkApp.ModManager.Selected = Move(-1);   
    }

    [RelayCommand]
    private static void MoveDown()
    {
        TkApp.ModManager.Selected = Move(1);
    }

    private static TkMod? Move(int direction)
    {
        if (TkApp.ModManager.Selected is not TkMod target) {
            Console.WriteLine(Chalk.BrightRed + "No selection to move.");
            return default;
        }
        
        int currentIndex = TkApp.ModManager.Mods.IndexOf(target);
        int newIndex = currentIndex + direction;

        if (newIndex < 0 || newIndex >= TkApp.ModManager.Mods.Count) {
            return target;
        }

        TkMod store = TkApp.ModManager.Mods[newIndex];
        TkApp.ModManager.Mods[newIndex] = target;
        TkApp.ModManager.Mods[currentIndex] = store;
        
        TkApp.ModManager.Save();

        return target;
    }
    
    [RelayCommand]
    private static async Task Merge()
    {
        await MergeAny(async (merger, changelogs) => {
            await Task.Run(() => {
                merger.Merge(changelogs);
            }).ConfigureAwait(false);
        });
    }
    
    [RelayCommand]
    private static async Task MergeParallel()
    {
        await MergeAny(async (merger, changelogs) => {
            await merger.MergeAsync(changelogs)
                .ConfigureAwait(false);
        });
    }
    
    [RelayCommand]
    private static async Task MergeAny(Func<TkMerger, IEnumerable<TkChangelog>, Task> run)
    {
        string outputFolderPath = Path.Combine(AppContext.BaseDirectory, ".merged");
        ITkModWriter writer = new FolderModWriter(
            outputFolderPath);
        
        TkMerger merger = new(writer, TkApp.TkRomProvider.GetRom());
        
        long startTime = Stopwatch.GetTimestamp();

        try {
            await run(merger, TkApp.ModManager
                .Mods
                .Select(x => x.Changelog)
                .Reverse()
            );
        }
        catch (Exception ex) {
            TkLog.Instance.LogError(ex, "Failed to merge mods.");
        }

        TimeSpan delta = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine(Chalk.Green + $"Elapsed time: {delta.TotalMilliseconds} ms");
    }
}