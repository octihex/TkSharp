using SharpCompress.Archives;
using TkSharp.Core;
using TkSharp.Core.IO.ModSources;
using TkSharp.Core.Models;
using TkSharp.Merging;

namespace TkSharp.IO.Readers;

public sealed class ArchiveModReader(ITkModWriterProvider writerProvider, ITkRomProvider romProvider) : ITkModReader
{
    private readonly ITkModWriterProvider _writerProvider = writerProvider;
    private readonly ITkRomProvider _romProvider = romProvider;

    public ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext context = default, CancellationToken ct = default)
    {
        if (input is not string fileName || stream is null) {
            return ValueTask.FromResult<TkMod?>(null);
        }
        
        using IArchive archive = ArchiveFactory.Open(stream);
        if (LocateRoot(archive) is not { Key: not null } root) {
            return ValueTask.FromResult<TkMod?>(null);
        }

        if (context.Id == Ulid.Empty) {
            context.Id = Ulid.NewUlid();
        }
        
        ArchiveModSource source = new(archive, root);
        ITkModWriter writer = _writerProvider.GetSystemWriter(context);

        TkChangelogBuilder builder = new(source, writer, _romProvider.GetRom());
        TkChangelog changelog = builder.Build();

        return ValueTask.FromResult<TkMod?>(new TkMod {
            Id = context.Id,
            Name = Path.GetFileNameWithoutExtension(fileName),
            Changelog = changelog
        });
    }

    public bool IsKnownInput(object? input)
    {
        return input is string path &&
               Path.GetExtension(path.AsSpan()) is ".zip" or ".rar";
    }
    
    private static IArchiveEntry? LocateRoot(IArchive archive)
    {
        IArchiveEntry? previous = null;
        foreach (IArchiveEntry entry in archive.Entries) {
            if (!entry.IsDirectory) {
                continue;
            }

            ReadOnlySpan<char> key = entry.Key.AsSpan();
            if (key.Length > 5 && Path.GetFileName(key[^1] is '/' or '\\' ? key[..^1] : key) is "romfs" or "exefs" or "cheats") {
                return previous;
            }

            previous = entry;
        }

        return null;
    }
}