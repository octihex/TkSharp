// ReSharper disable CheckNamespace

using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp;

public abstract partial class TkItem : ObservableObject
{
    /// <summary>
    /// The name of this item.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// The description of this item.
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// The thumbnail of this item.
    /// </summary>
    [ObservableProperty]
    private Core.Models.TkThumbnail? _thumbnail;
}