using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TkSharp.Core.Common;

namespace TkSharp.Core.Models;

public sealed partial class TkModOption : TkStoredItem
{
    private TkProfileOptionStateLookup? _profileStateStorage;

    public TkProfileOptionStateLookup StateLookup
        => _profileStateStorage ?? throw new InvalidOperationException("Profile state storage is not initialized.");

    [ObservableProperty]
    private int _priority = -1;

    public bool IsEnabled {
        get => StateLookup.GetIsEnabled();
        set {
            OnPropertyChanging();
            StateLookup.SetIsEnabled(value);
            OnPropertyChanged();
        }
    }

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