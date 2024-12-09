using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using TkSharp.DevTools.ViewModels;
using TkSharp.DevTools.Views;

namespace TkSharp.DevTools;

public class App : Application
{
    public static IStorageProvider Storage { get; private set; } = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
            return;
        }
        
        BindingPlugins.DataValidators.RemoveAt(0);
        
        desktop.MainWindow = new ShellView {
            DataContext = new ShellViewModel()
        };

        Storage = desktop.MainWindow.StorageProvider;

        base.OnFrameworkInitializationCompleted();
    }
}