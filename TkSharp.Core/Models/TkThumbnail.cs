using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

public partial class TkThumbnail : ObservableObject
{
    public static Func<Stream, object>? CreateBitmap { get; set; }
    
    [ObservableProperty]
    private string _thumbnailPath = ".thumbnail";
    
    [ObservableProperty]
    [property: JsonIgnore]
    private object? _bitmap;

    [JsonIgnore]
    public bool IsResolved { get; set; }
}