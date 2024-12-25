using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

public sealed partial class TkMod : TkStoredItem
{
    /// <summary>
    /// The version of this mod.
    /// </summary>
    [ObservableProperty]
    private string _version = string.Empty;
    
    /// <summary>
    /// The author of this mod.
    /// </summary>
    [ObservableProperty]
    private string _author = string.Empty;

    /// <summary>
    /// The contributors of this mod.
    /// </summary>
    public ObservableCollection<TkModContributor> Contributors { get; init; } = [];

    /// <summary>
    /// The option groups in this mod.
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<TkModOptionGroup> OptionGroups { get; init; } = [];

    /// <summary>
    /// The dependencies of this mod.
    /// </summary>
    public ObservableCollection<TkModDependency> Dependencies { get; init; } = [];

    public TkProfileMod GetProfileMod()
    {
        return new TkProfileMod(this);
    }
}