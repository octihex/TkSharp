namespace TkSharp.Core.IO;

public sealed class FolderModSource(string sourceFolder) : ITkModSource
{
    public string PathToRoot { get; } = sourceFolder;

    public string[] Files { get; } = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);

    public Stream OpenRead(string file)
    {
        return File.OpenRead(file);
    }
}