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
        ReadOnlySpan<char> romfs = [];
        
        foreach (IArchiveEntry entry in archive.Entries) {
            if (!entry.IsDirectory) {
                continue;
            }

            ReadOnlySpan<char> key = entry.Key.AsSpan();
            if (key.Length < 5) {
                continue;
            }
            
            ReadOnlySpan<char> normalizedKey = (key[^1] is '/' or '\\' ? key[..^1] : key)
                .ToString()
                .ToLower();
            
            if (normalizedKey[..5] is "romfs" or "exefs" || normalizedKey[..6] is "cheats") {
                root = null;
                return true;
            }
            
            if (Path.GetFileName(normalizedKey) is "romfs" or "exefs" or "cheats") {
                romfs = key;
                break;
            }
        }

        if (romfs.IsEmpty) {
            root = null;
            return false;
        }
        
        foreach (IArchiveEntry entry in archive.Entries) {
            if (!entry.IsDirectory) {
                continue;
            }

            ReadOnlySpan<char> key = entry.Key.AsSpan();

            if (romfs.Length > key.Length && romfs[..key.Length].SequenceEqual(key)) {
                root = entry;
                return true;
            }
        }

        root = null;
        return false;
    }
}