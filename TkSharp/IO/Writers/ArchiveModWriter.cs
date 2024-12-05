using SharpCompress.Common;
using SharpCompress.Writers.Zip;
using TkSharp.Core;

namespace TkSharp.IO.Writers;

public sealed class ArchiveModWriter(Stream output) : ITkModWriter
{
    private static readonly ZipWriterEntryOptions _defaultEntryOptions = new();
    private readonly ZipWriter _writer = new(output, new ZipWriterOptions(CompressionType.BZip2));
    
    public Stream OpenWrite(string filePath)
    {
        return _writer.WriteToStream(filePath, _defaultEntryOptions);
    }

    public ITkModSource GetSource()
    {
        throw new NotSupportedException("Getting the source from an archive writer is not supported.");
    }
}