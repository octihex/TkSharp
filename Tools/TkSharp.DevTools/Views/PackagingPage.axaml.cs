using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using TkSharp.DevTools.ViewModels;
using TkSharp.Packaging;

namespace TkSharp.DevTools.Views;

public partial class PackagingPage : UserControl
{
    public PackagingPage()
    {
        InitializeComponent();
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not PackagingPageViewModel vm) {
            return;
        }
        
        if (e.Source is ContentPresenter { Content: TkProject project }) {
            vm.Project = project;
        }
    }
}