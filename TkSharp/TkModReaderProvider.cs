using TkSharp.Core;
using TkSharp.IO.Readers;

namespace TkSharp;

public class TkModReaderProvider(ITkSystemProvider tkWriterProvider, ITkRomProvider tkRomProvider)
    : ITkModReaderProvider
{
    private readonly List<ITkModReader> _readers = [
        new ArchiveModReader(tkWriterProvider, tkRomProvider),
        new FolderModReader(tkWriterProvider, tkRomProvider),
        new SevenZipModReader(tkWriterProvider, tkRomProvider),
        new TkPackReader(tkWriterProvider),
    ];

    public void Register(ITkModReader reader)
    {
        _readers.Add(reader);
    }

    public ITkModReader? GetReader(object input)
    {
        return _readers.FirstOrDefault(reader => reader.IsKnownInput(input));
    }

    public bool CanRead(object input)
    {
        return _readers.Any(reader => reader.IsKnownInput(input));
    }
}