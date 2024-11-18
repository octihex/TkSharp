namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public interface IBymlArrayChangelogBuilderProvider
{
    IBymlArrayChangelogBuilder GetChangelogBuilder(ref BymlTrackingInfo info, ReadOnlySpan<char> key);
}