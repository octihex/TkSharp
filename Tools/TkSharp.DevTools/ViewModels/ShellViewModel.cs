using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using TkSharp.DevTools.Views;

namespace TkSharp.DevTools.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private static readonly MergingPage _mergingPage = new() {
        DataContext = new MergingPageViewModel()
    };
    
    private static readonly PackagingPage _packagingPage = new() {
        DataContext = new PackagingPageViewModel()
    };
    
    [ObservableProperty]
    private object? _pageContent = _mergingPage;
    
    [ObservableProperty]
    private NavigationViewItem? _currentPageItem;

    partial void OnCurrentPageItemChanged(NavigationViewItem? value)
    {
        PageContent = value?.Tag switch {
            nameof(PackagingPage) => _packagingPage,
            _ => _mergingPage
        };
    }
}