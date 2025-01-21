using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace TkSharp.Core.Models;

public sealed partial class TkProfile : TkItem
{
    [ObservableProperty]
    private TkProfileMod? _selected;
    
    public ObservableCollection<TkProfileMod> Mods { get; } = [];

    public TkProfile()
    {
    }

    [JsonConstructor]
    private TkProfile(TkProfileMod? selected, ObservableCollection<TkProfileMod> mods)
    {
        _selected = selected;
        Mods = mods;
    }

    /// <summary>
    /// Moves the <see cref="Selected"/> mod up in the <see cref="Mods"/> collection.
    /// </summary>
    public TkProfileMod? MoveUp() => MoveUp(Selected);
    
    /// <summary>
    /// Moves the <paramref name="target"/> up in the <see cref="Mods"/> collection.
    /// </summary>
    /// <param name="target">The target <see cref="TkMod"/> to be repositioned.</param>
    public TkProfileMod? MoveUp(TkProfileMod? target) => Move(target, direction: -1);
    
    /// <summary>
    /// Moves the <see cref="Selected"/> mod up in the <see cref="Mods"/> collection.
    /// </summary>
    public TkProfileMod? MoveDown() => MoveDown(Selected);

    /// <summary>
    /// Moves the <paramref name="target"/> down in the <see cref="Mods"/> collection.
    /// </summary>
    /// <param name="target">The target <see cref="TkMod"/> to be repositioned.</param>
    public TkProfileMod? MoveDown(TkProfileMod? target) => Move(target, direction: 1);

    /// <summary>
    /// Move the <paramref name="target"/> in the provided <paramref name="direction"/>.<br/>
    /// <i><b>Note:</b> <c>0</c> is the highest position.</i>
    /// </summary>
    /// <param name="target">The target <see cref="TkMod"/> to be repositioned.</param>
    /// <param name="direction">The direction to move.</param>
    private TkProfileMod? Move(TkProfileMod? target, int direction)
    {
        if (target is null) {
            return target;
        }
        
        int currentIndex = Mods.IndexOf(target);
        int newIndex = currentIndex + direction;

        if (newIndex < 0 || newIndex >= Mods.Count) {
            return target;
        }

        TkProfileMod store = Mods[newIndex];
        Mods[newIndex] = target;
        Mods[currentIndex] = store;
        
        return target;
    }

    public void RebaseOptions(TkProfileMod? target = null)
    {
        target ??= Selected;
        
        if (target is not { } mod || !Mods.Contains(mod)) {
            return;
        }

        TkLog.Instance.LogDebug(
            "Rebasing to '{ModName}' in '{ProfileName}'", mod.Mod.Name, Name);
        
        foreach (TkModOptionGroup group in mod.Mod.OptionGroups) {
            foreach (TkModOption option in group.Options) {
                option.InitializeProfileState(group, mod);
            }
        }
    }

    partial void OnSelectedChanged(TkProfileMod? value) => RebaseOptions(value);
}