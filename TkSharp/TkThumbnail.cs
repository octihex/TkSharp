using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp;

public partial class TkThumbnail : ObservableObject
{
    [ObservableProperty]
    private string _thumbnailPath = ".thumbnail";
    
    [ObservableProperty]
    [property: JsonIgnore]
    private object? _bitmap;

    [JsonIgnore]
    public bool IsResolved { get; set; }
}