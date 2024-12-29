using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

public sealed partial class TkProfileMod(TkMod mod) : ObservableObject
{
    public Dictionary<TkModOptionGroup, ObservableCollection<TkModOption>> SelectedOptions { get; set; } = [];
    
    [ObservableProperty]
    private TkMod _mod = mod;
    
    [ObservableProperty]
    private bool _isEnabled = true;
    
    [ObservableProperty]
    private bool _isEditingOptions;
}