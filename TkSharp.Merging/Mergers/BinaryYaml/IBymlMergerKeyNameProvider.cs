namespace TkSharp.Merging.Mergers.BinaryYaml;

public interface IBymlMergerKeyNameProvider
{
    string? GetKeyName(ReadOnlySpan<char> key, ReadOnlySpan<char> type, int depth);
}