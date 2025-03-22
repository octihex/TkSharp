using System;
using System.IO;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using TkSharp.Core.IO.Serialization;
using TkSharp.Core.IO.Serialization.Models;
using TkSharp.Core.Models;

namespace TkSharp.IO.Serialization;

internal static class TkModManagerSerializer
{
    private const uint MAGIC = 0x4D4D4B54;
    private const uint VERSION = 0x10100000;
    
    public static void Write(in Stream output, TkModManager manager)
    {
        output.Write(MAGIC);
        output.Write(VERSION);

        TkLookupContext lookup = new();

        output.Write(manager.Mods.Count);
        for (int i = 0; i < manager.Mods.Count; i++) {
            TkMod mod = manager.Mods[i];
            TkBinaryWriter.WriteTkMod(output, mod, lookup);
            lookup.Mods[mod] = i;
        }
        
        output.Write(manager.Profiles.Count);
        foreach (TkProfile profile in manager.Profiles) {
            TkBinaryWriter.WriteTkProfile(output, profile, lookup);
        }
        
        output.Write(manager.Selected is not null
            ? lookup.Mods[manager.Selected]
            : -1);
        
        output.Write(manager.CurrentProfile is not null
            ? manager.Profiles.IndexOf(manager.CurrentProfile)
            : -1);
    }
    
    public static TkModManager Read(in Stream input, string dataFolderPath)
    {
        TkModManager manager = new(dataFolderPath);

        if (input.Read<uint>() != MAGIC) {
            throw new InvalidDataException("Invalid mod manager magic.");
        }

        if (input.Read<uint>() != VERSION) {
            throw new InvalidDataException("Invalid mod manager version, expected 1.1.0.");
        }

        int modCount = input.Read<int>();
        for (int i = 0; i < modCount; i++) {
            manager.Mods.Add(
                TkBinaryReader.ReadTkMod(new TkModContext(), input, manager)
            );
        }
        
        int profileCount = input.Read<int>();
        for (int i = 0; i < profileCount; i++) {
            manager.Profiles.Add(
                TkBinaryReader.ReadTkProfile(input, manager.Mods)
            );
        }
        
        int selectedModIndex = input.Read<int>();
        if (selectedModIndex > -1) {
            manager.Selected = manager.Mods[selectedModIndex];
        }
        
        int currentProfileIndex = input.Read<int>();
        if (currentProfileIndex > -1) {
            manager.CurrentProfile = manager.Profiles[currentProfileIndex];
        }

        manager.Unlock();

        using (StreamWriter streamWriter = new StreamWriter("zstate.db.json"))
		{
			JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
			{
				WriteIndented = true
			};

			streamWriter.WriteLine(JsonSerializer.Serialize<TkModManager>(manager, jsonSerializerOptions));
		}

        return manager;
    }
}