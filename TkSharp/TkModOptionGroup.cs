using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp;

public enum OptionGroupType
{
    Multi,
    MultiRequired,
    Single,
    SingleRequired
}

public sealed partial class TkModOptionGroup : TkItem
{
    [ObservableProperty]
    private OptionGroupType _type;

    [ObservableProperty]
    private string? _iconName;

    public ObservableCollection<TkModOption> Options { get; } = [];

    public ObservableCollection<TkModOption> DefaultSelectedOptions { get; } = [];

    public ObservableCollection<TkModDependency> Dependencies { get; } = [];

    public TkModOptionGroup()
    {
    }
}