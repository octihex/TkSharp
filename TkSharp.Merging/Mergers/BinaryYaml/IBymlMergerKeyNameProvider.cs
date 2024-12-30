using TkSharp.Merging.Common.BinaryYaml;

namespace TkSharp.Merging.Mergers.BinaryYaml;

public interface IBymlMergerKeyNameProvider
{
    BymlKeyName GetKeyName(ReadOnlySpan<char> key, ReadOnlySpan<char> type, int depth);
}