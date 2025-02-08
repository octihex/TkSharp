using TkSharp.Core;

namespace TkSharp.IO;

public sealed class TkSystemSource(string rootFolderPath) : ITkSystemSource
{
    public Stream OpenRead(string relativeFilePath)
    {
        return File.OpenRead(Path.Combine(rootFolderPath, relativeFilePath));
    }

    public bool Exists(string relativeFilePath)
    {
        return File.Exists(Path.Combine(rootFolderPath, relativeFilePath));
    }

    public ITkSystemSource GetRelative(string relativeSourcePath)
    {
        return new TkSystemSource(
            Path.Combine(rootFolderPath, relativeSourcePath)
        );
    }
}