using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp;

public sealed partial class TkMod : TkStoredItem
{
    /// <summary>
    /// The version of this mod.
    /// </summary>
    [ObservableProperty]
    private int _version;
    
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
    public ObservableCollection<TkModOptionGroup> OptionGroups { get; init; } = [];

    /// <summary>
    /// The dependencies of this mod.
    /// </summary>
    public ObservableCollection<TkModDependency> Dependencies { get; init; } = [];
}