using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

public sealed partial class TkModDependency : ObservableObject
{
    /// <summary>
    /// The name of the dependent <see cref="TkItem"/>.
    /// </summary>
    [ObservableProperty]
    private string _dependentName = string.Empty;

    /// <summary>
    /// The ID of the dependent <see cref="TkStoredItem"/>.
    /// </summary>
    [ObservableProperty]
    private Ulid _dependentId;
}