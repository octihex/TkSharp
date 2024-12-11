namespace TkSharp.Merging.ChangelogBuilders.BinaryYaml;

public ref struct BymlTrackingInfo(ReadOnlySpan<char> canonical, int level)
{
    public ReadOnlySpan<char> Type = Path.GetExtension(
        Path.GetFileNameWithoutExtension(canonical))[1..];
    
    public int Level = level;
}