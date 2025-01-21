using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TkSharp.Core.Common;

namespace TkSharp.Core.Models;

public sealed partial class TkModOption : TkStoredItem
{
    private TkProfileOptionStateLookup? _profileStateStorage;

    public TkProfileOptionStateLookup StateLookup
        => _profileStateStorage; // ?? throw new InvalidOperationException("Profile state storage is not initialized.");
        // TODO: Properly handle when this is null (e.g. packaging a mod without it being installed)

    [ObservableProperty]
    private int _priority = -1;

    public bool IsEnabled {
        get => StateLookup?.GetIsEnabled() ?? false;
        set {
            if (StateLookup == null) return;
            OnPropertyChanging();
            StateLookup.SetIsEnabled(value);
            OnPropertyChanged();
        }
    }

    public bool CanChangeState => StateLookup?.CanChangeState() ?? false;

    public void InitializeProfileState(TkModOptionGroup group, TkProfileMod parent)
    {
        _profileStateStorage = new TkProfileOptionStateLookup(this, group, parent);
    }

    public void UpdateState()
    {
        OnPropertyChanged(nameof(CanChangeState));
    }
}