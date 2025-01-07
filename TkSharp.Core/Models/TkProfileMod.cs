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

    public override bool Equals(object? obj)
    {
        if (obj is not TkProfileMod profileMod) {
            return false;
        }

        return profileMod.Mod.Id == Mod.Id;
    }

    public override int GetHashCode()
    {
        return Mod.GetHashCode();
    }
}