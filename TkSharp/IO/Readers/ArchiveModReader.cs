using SharpCompress.Archives;
using TkSharp.Core;
using TkSharp.Core.IO.ModSources;
using TkSharp.Core.Models;
using TkSharp.Merging;

namespace TkSharp.IO.Readers;

public sealed class ArchiveModReader(ITkSystemProvider systemProvider, ITkRomProvider romProvider) : ITkModReader
{
    private readonly ITkSystemProvider _systemProvider = systemProvider;
    private readonly ITkRomProvider _romProvider = romProvider;

    public async ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext context = default, CancellationToken ct = default)
    {
        if (input is not string fileName || stream is null) {
            return null;
        }
        
        using IArchive archive = ArchiveFactory.Open(stream);
        if (!LocateRoot(archive, out IArchiveEntry? root)) {
            return null;
        }

        if (context.Id == Ulid.Empty) {
            context.Id = Ulid.NewUlid();
        }
        
        ArchiveModSource source = new(archive, root);
        ITkModWriter writer = _systemProvider.GetSystemWriter(context);

        TkChangelogBuilder builder = new(source, writer, _romProvider.GetRom(),
            _systemProvider.GetSystemSource(context.Id.ToString())
        );
        
        TkChangelog changelog = await builder.BuildAsync(ct)
            .ConfigureAwait(false);

        return new TkMod {
            Id = context.Id,
            Name = Path.GetFileNameWithoutExtension(fileName),
            Changelog = changelog
        };
    }

    public bool IsKnownInput(object? input)
    {
        return input is string path &&
               Path.GetExtension(path.AsSpan()) is ".zip" or ".rar";
    }
    
    internal static bool LocateRoot(IArchive archive, out IArchiveEntry? root)
    {
        IArchiveEntry? previous = null;
        foreach (IArchiveEntry entry in archive.Entries) {
            if (!entry.IsDirectory) {
                continue;
            }

            ReadOnlySpan<char> key = entry.Key.AsSpan();
            if (key.Length > 5 && Path.GetFileName(key[^1] is '/' or '\\' ? key[..^1] : key) is "romfs" or "exefs" or "cheats") {
                root = previous;
                return true;
            }

            previous = entry;
        }

        root = default;
        return false;
    }
}