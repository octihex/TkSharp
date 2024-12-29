using CommunityToolkit.Mvvm.ComponentModel;

namespace TkSharp.Core.Models;

public sealed partial class TkModOption : TkStoredItem
{
    [ObservableProperty]
    private int _priority = -1;
}