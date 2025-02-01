using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using TkSharp.Core.Common;

namespace TkSharp.Core.Models;

public sealed partial class TkModOption : TkStoredItem
{
    private TkProfileOptionStateLookup? _profileStateStorage;

    [JsonIgnore]
    public TkProfileOptionStateLookup StateLookup
        => _profileStateStorage ?? throw new InvalidOperationException("Profile state storage is not initialized.");

    [ObservableProperty]
    private int _priority = -1;

    [JsonIgnore]
    public bool IsEnabled {
        get => StateLookup.GetIsEnabled();
        set {
            OnPropertyChanging();
            StateLookup.SetIsEnabled(value);
            OnPropertyChanged();
            TkProfile.OnStateChanged();
        }
    }

    [JsonIgnore]
    public bool CanChangeState => StateLookup.CanChangeState();

    public void InitializeProfileState(TkModOptionGroup group, TkProfileMod parent)
    {
        _profileStateStorage = new TkProfileOptionStateLookup(this, group, parent);
    }

    public void UpdateState()
    {
        OnPropertyChanged(nameof(CanChangeState));
    }
}