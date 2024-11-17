using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;

namespace TkSharp.Merging.ChangelogBuilders.GameData;

public class GameDataIndex
{
    private static readonly FrozenDictionary<ulong, FrozenDictionary<uint, int>> _lookup;
    private static readonly FrozenDictionary<ulong, int> _lookupUInt64;

    static GameDataIndex()
    {
        using Stream stream = typeof(GameDataIndex).Assembly
            .GetManifestResourceStream("TkSharp.Merging.Resources.GameDataIndex.bin")!;

        Dictionary<ulong, FrozenDictionary<uint, int>> lookup = [];

        int count = stream.Read<int>();
        for (int i = 0; i < count; i++) {
            ulong tableNameHash = stream.Read<ulong>();
            int entryCount = stream.Read<int>();
            lookup.Add(tableNameHash,
                ReadEntries(stream, entryCount)
            );
        }

        _lookup = lookup.ToFrozenDictionary();
        
        int u64EntryCount = stream.Read<int>();
        _lookupUInt64 = ReadEntriesUInt64(stream, u64EntryCount);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetIndex(ulong tableNameHash, uint hash, out int index)
    {
        return _lookup[tableNameHash].TryGetValue(hash, out index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetIndex(ulong hash, out int index)
    {
        return _lookupUInt64.TryGetValue(hash, out index);
    }
    
    private static FrozenDictionary<uint, int> ReadEntries(Stream stream, int count)
    {
        Dictionary<uint, int> entries = [];
        for (int i = 0; i < count; i++) {
            entries.Add(stream.Read<uint>(), stream.Read<int>());
        }

        return entries.ToFrozenDictionary();
    }
    
    private static FrozenDictionary<ulong, int> ReadEntriesUInt64(Stream stream, int count)
    {
        Dictionary<ulong, int> entries = [];
        for (int i = 0; i < count; i++) {
            entries.Add(stream.Read<ulong>(), stream.Read<int>());
        }

        return entries.ToFrozenDictionary();
    }
}