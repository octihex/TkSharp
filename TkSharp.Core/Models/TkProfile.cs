using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

public sealed partial class TkProfile : TkItem
{
    [ObservableProperty]
    private TkProfileMod? _selected;
    
    public ObservableCollection<TkProfileMod> Mods { get; } = [];

    public TkProfile()
    {
    }

    [JsonConstructor]
    private TkProfile(TkProfileMod? selected, ObservableCollection<TkProfileMod> mods)
    {
        _selected = selected;
        Mods = mods;
    }
}