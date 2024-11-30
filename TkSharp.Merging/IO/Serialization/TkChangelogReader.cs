using CommunityToolkit.HighPerformance;
using TkSharp.Core;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;
using static TkSharp.Merging.IO.Serialization.TkChangelogWriter;

namespace TkSharp.Merging.IO.Serialization;

public static class TkChangelogReader
{
    public static TkChangelog Read(in Stream input, ITkSystemSource? source)
    {
        if (input.Read<uint>() != MAGIC) {
            throw new InvalidDataException(
                "Invalid totk changelog magic.");
        }

        var result = new TkChangelog {
            BuilderVersion = input.Read<int>(),
            GameVersion = input.Read<int>(),
            Source = source
        };
        
        int changelogFileCount = input.Read<int>();
        for (int i = 0; i < changelogFileCount; i++) {
            result.ChangelogFiles.Add(
                new TkChangelogEntry(
                    input.ReadString()!,
                    input.Read<ChangelogEntryType>(),
                    input.Read<TkFileAttributes>(),
                    input.Read<int>()
                )
            );
        }

        ReadFileList(input, result.MalsFiles);
        
        int patchFileCount = input.Read<int>();
        for (int i = 0; i < patchFileCount; i++) {
            result.PatchFiles.Add(
                ReadTkPatch(input)
            );
        }
        
        ReadFileList(input, result.SubSdkFiles);
        ReadFileList(input, result.CheatFiles);
        
        return result;
    }

    private static TkPatch ReadTkPatch(in Stream input)
    {
        string nsoBinaryId = input.ReadString()!;
        var result = new TkPatch(nsoBinaryId);
        
        int entryCount = input.Read<int>();
        for (int i = 0; i < entryCount; i++) {
            result.Entries[input.Read<uint>()] = input.Read<uint>();
        }

        return result;
    }

    private static void ReadFileList(in Stream input, IList<string> result)
    {
        int count = input.Read<int>();
        for (int i = 0; i < count; i++) {
            result.Add(input.ReadString()!);
        }
    }
}