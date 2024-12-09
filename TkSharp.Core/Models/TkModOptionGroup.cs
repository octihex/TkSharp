using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

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

    [JsonIgnore]
    public ObservableCollection<TkModOption> Options { get; } = [];

    [JsonIgnore]
    public ObservableCollection<TkModOption> DefaultSelectedOptions { get; } = [];

    public ObservableCollection<TkModDependency> Dependencies { get; } = [];

    public TkModOptionGroup()
    {
    }
}