using System.Runtime.CompilerServices;
using TkSharp.Merging.ChangelogBuilders.BinaryYaml;

namespace TkSharp.Merging.ChangelogBuilders.GameData;

public class GameDataStructArrayChangelogBuilderProvider : Singleton<GameDataStructArrayChangelogBuilderProvider>, IBymlArrayChangelogBuilderProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBymlArrayChangelogBuilder GetChangelogBuilder(ref BymlTrackingInfo info, ReadOnlySpan<char> key)
    {
        return key switch {
            "DefaultValue" => new BymlKeyedArrayChangelogBuilder("Hash"),
            _ => BymlArrayChangelogBuilder.Instance
        };
    }
}