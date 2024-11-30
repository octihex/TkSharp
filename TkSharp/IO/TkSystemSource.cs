using TkSharp.Core;

namespace TkSharp.IO;

public sealed class TkSystemSource(string rootFolderPath) : ITkSystemSource
{
    public Stream OpenRead(string relativeFilePath)
    {
        return File.OpenRead(Path.Combine(rootFolderPath, relativeFilePath));
    }
}