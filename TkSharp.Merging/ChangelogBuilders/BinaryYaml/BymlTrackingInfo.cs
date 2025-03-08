namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public ref struct BymlTrackingInfo(ReadOnlySpan<char> canonical, int depth)
{
    public ReadOnlySpan<char> Type = Path.GetExtension(
        Path.GetFileNameWithoutExtension(canonical)) is { Length: > 0 } type
        ? type[1..]
        : [];

    public int Depth = depth;
}