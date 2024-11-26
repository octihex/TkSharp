using System.Runtime.CompilerServices;

namespace TkSharp.Core.IO.ModSources;

public sealed class FolderModSource(string sourceFolder) : TkModSourceBase<string>(sourceFolder)
{
    protected override IEnumerable<string> Files { get; }
        = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);

    protected override Stream OpenRead(string file)
    {
        return File.OpenRead(file);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string GetFileName(string input)
    {
        return input;
    }
}