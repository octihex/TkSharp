using System.IO.Compression;
using TkSharp.Core;
using TkSharp.Core.Extensions;

namespace TkSharp.Packaging.IO.Writers;

public sealed class ArchiveModWriter : ITkModWriter
{
    private readonly Dictionary<string, MemoryStream> _entries = [];
    
    public Stream OpenWrite(string filePath)
    {
        return _entries[filePath] = new MemoryStream();
    }

    public void Compile(Stream output)
    {
        using ZipArchive writer = new(output, ZipArchiveMode.Create);

        foreach ((string fileName, MemoryStream ms) in _entries.OrderBy(entry => entry.Key)) {
            using Stream entry = writer.CreateEntry(fileName).Open();
            entry.Write(ms.GetSpan());
            ms.Dispose();
        }
    }
}