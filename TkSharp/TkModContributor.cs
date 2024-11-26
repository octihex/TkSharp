using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp;

[method: JsonConstructor]
public sealed partial class TkModContributor(string author, string contribution) : ObservableObject
{
    [ObservableProperty]
    private string _author = author;
    
    [ObservableProperty]
    private string _contribution = contribution;
}