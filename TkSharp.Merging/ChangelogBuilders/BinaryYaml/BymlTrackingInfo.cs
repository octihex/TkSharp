namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public ref struct BymlTrackingInfo(ReadOnlySpan<char> canonical, int level)
{
    public readonly ReadOnlySpan<char> Type = Path.GetExtension(
        Path.GetFileNameWithoutExtension(canonical));
    
    public int Level = level;
}