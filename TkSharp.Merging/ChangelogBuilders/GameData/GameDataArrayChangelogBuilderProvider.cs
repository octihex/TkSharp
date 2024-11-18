using System.Runtime.CompilerServices;
using TkSharp.Merging.ChangelogBuilders.BinaryYaml;

namespace TkSharp.Merging.ChangelogBuilders.GameData;

public class GameDataArrayChangelogBuilderProvider : Singleton<GameDataArrayChangelogBuilderProvider>, IBymlArrayChangelogBuilderProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBymlArrayChangelogBuilder GetChangelogBuilder(ref BymlTrackingInfo info, ReadOnlySpan<char> key)
    {
        return BymlDirectIndexArrayChangelogBuilder.Instance;
    }
}