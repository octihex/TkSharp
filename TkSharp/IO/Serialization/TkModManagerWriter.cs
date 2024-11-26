using CommunityToolkit.HighPerformance;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;
using TkSharp.Merging.IO;
using TkSharp.Merging.IO.Serialization;

namespace TkSharp.IO.Serialization;

public static class TkModManagerWriter
{
    internal const uint MAGIC = 0x4D4D4B54;
    internal const uint VERSION = 0x10100000;

    public static void Write(in Stream output, TkModManager manager)
    {
        output.Write(MAGIC);
        output.Write(VERSION);

        Dictionary<TkMod, int> modsIndexLookup = [];

        output.Write(manager.Mods.Count);
        for (int i = 0; i < manager.Mods.Count; i++) {
            TkMod mod = manager.Mods[i];
            WriteTkMod(output, mod);
            modsIndexLookup[mod] = i;
        }
        
        output.Write(manager.Profiles.Count);
        foreach (TkProfile profile in manager.Profiles) {
            WriteTkProfile(output, profile, modsIndexLookup);
        }
        
        output.Write(manager.Selected is not null
            ? modsIndexLookup[manager.Selected]
            : -1);
        
        output.Write(manager.CurrentProfile is not null
            ? manager.Profiles.IndexOf(manager.CurrentProfile)
            : -1);
    }

    private static void WriteTkMod(in Stream output, in TkMod mod)
    {
        WriteTkStoredItem(output, mod);
        output.WriteString(mod.Version);
        output.WriteString(mod.Author);

        output.Write(mod.Contributors.Count);
        foreach (TkModContributor contributor in mod.Contributors) {
            output.WriteString(contributor.Author);
            output.WriteString(contributor.Contribution);
        }

        output.Write(mod.OptionGroups.Count);
        foreach (TkModOptionGroup optionGroup in mod.OptionGroups) {
            WriteTkModOptionGroup(output, optionGroup);
        }

        output.Write(mod.Dependencies.Count);
        foreach (TkModDependency dependency in mod.Dependencies) {
            WriteTkModDependency(output, dependency);
        }
    }

    private static void WriteTkModOptionGroup(in Stream output, in TkModOptionGroup optionGroup)
    {
        WriteTkItem(output, optionGroup);
        output.Write(optionGroup.Type);
        output.WriteString(optionGroup.IconName);

        Dictionary<TkModOption, int> indexLookup = [];
        
        output.Write(optionGroup.Options.Count);
        for (int i = 0; i < optionGroup.Options.Count; i++) {
            TkModOption option = optionGroup.Options[i];
            WriteTkStoredItem(output, option);
            indexLookup[option] = i;
        }

        output.Write(optionGroup.DefaultSelectedOptions.Count);
        foreach (TkModOption selectedOptionIndex in optionGroup.DefaultSelectedOptions) {
            output.Write(indexLookup[selectedOptionIndex]);
        }

        output.Write(optionGroup.Dependencies.Count);
        foreach (TkModDependency dependency in optionGroup.Dependencies) {
            WriteTkModDependency(output, dependency);
        }
    }

    private static void WriteTkModDependency(in Stream output, in TkModDependency dependency)
    {
        output.WriteString(dependency.DependentName);
        output.Write(dependency.DependentId);
    }

    private static void WriteTkProfile(in Stream output, TkProfile profile, Dictionary<TkMod, int> modsIndexLookup)
    {
        WriteTkItem(output, profile);
        
        output.Write(profile.Mods.Count);
        foreach (TkProfileMod mod in profile.Mods) {
            output.Write(modsIndexLookup[mod.Mod]);
            output.Write(mod.IsEnabled);
            output.Write(mod.IsEditingOptions);
        }
        
        
        output.Write(
            profile.Selected is not null ? modsIndexLookup[profile.Selected.Mod] : -1
        );
    }

    private static void WriteTkStoredItem(in Stream output, in TkStoredItem item)
    {
        output.Write(item.Id);
        WriteTkItem(output, item);
        TkChangelogWriter.Write(output, item.Changelog);
    }

    private static void WriteTkItem(in Stream output, in TkItem item)
    {
        output.WriteString(item.Name);
        output.WriteString(item.Description);
        WriteTkThumbnail(output, item.Thumbnail);
    }

    private static void WriteTkThumbnail(in Stream output, in TkThumbnail? thumbnail)
    {
        output.Write(thumbnail is not null);
        
        if (thumbnail is not null) {
            output.WriteString(thumbnail.ThumbnailPath);
        }
    }
}