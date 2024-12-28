using CommunityToolkit.HighPerformance;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;

namespace TkSharp.Core.IO.Serialization;

public static class TkBinaryWriter
{
    public const uint TKPK_MAGIC = 0x504D4B54;
    public const uint TKPK_VERSION = 0x10;
    
    public static void WriteTkMod(in Stream output, in TkMod mod)
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

    public static void WriteTkModOptionGroup(in Stream output, in TkModOptionGroup optionGroup)
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

    public static void WriteTkModDependency(in Stream output, in TkModDependency dependency)
    {
        output.WriteString(dependency.DependentName);
        output.Write(dependency.DependentId);
    }

    public static void WriteTkProfile(in Stream output, TkProfile profile, Dictionary<TkMod, int> modsIndexLookup)
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

    public static void WriteTkStoredItem(in Stream output, in TkStoredItem item)
    {
        output.Write(item.Id);
        WriteTkItem(output, item);
        TkChangelogWriter.Write(output, item.Changelog);
    }

    public static void WriteTkItem(in Stream output, in TkItem item)
    {
        output.WriteString(item.Name);
        output.WriteString(item.Description);
        WriteTkThumbnail(output, item.Thumbnail);
    }

    public static void WriteTkThumbnail(in Stream output, in TkThumbnail? thumbnail)
    {   
        output.Write(!(thumbnail is null || thumbnail.IsDefault));
        
        if (thumbnail is not null) {
            output.WriteString(thumbnail.ThumbnailPath);
        }
    }
}