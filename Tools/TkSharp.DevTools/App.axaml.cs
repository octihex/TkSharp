using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using TkSharp.DevTools.ViewModels;
using TkSharp.DevTools.Views;

namespace TkSharp.DevTools;

public class App : Application
{
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

        base.OnFrameworkInitializationCompleted();
    }
}