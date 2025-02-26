using System.Buffers;
using SharpCompress.Archives;
using TkSharp.Core;
using TkSharp.Core.IO.ModSources;
using TkSharp.Core.Models;
using TkSharp.Merging;

namespace TkSharp.IO.Readers;

public sealed class ArchiveModReader(ITkSystemProvider systemProvider, ITkRomProvider romProvider) : ITkModReader
{
    private static readonly SearchValues<string> _validFoldersSearchValues = SearchValues.Create(
        ["romfs", "exefs", "cheats", "extras"], StringComparison.OrdinalIgnoreCase);
    
    private readonly ITkSystemProvider _systemProvider = systemProvider;
    private readonly ITkRomProvider _romProvider = romProvider;

    public async ValueTask<TkMod?> ReadMod(TkModContext context, CancellationToken ct = default)
    {
        if (context.Input is not string fileName || context.Stream is null) {
            return null;
        }
        
        using IArchive archive = ArchiveFactory.Open(context.Stream);
        if (!LocateRoot(archive, out string? root)) {
            return null;
        }

        context.EnsureId();
        
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
    
    internal static bool LocateRoot(IArchive archive, out string? root)
    {
        foreach (IArchiveEntry entry in archive.Entries) {
            ReadOnlySpan<char> key = entry.Key.AsSpan();
            ReadOnlySpan<char> normalizedKey = key[^1] is '/' or '\\' ? key[..^1] : key;
            
            if (normalizedKey.Length < 5) {
                continue;
            }
            
            ReadOnlySpan<char> normalizedKeyLowercase = normalizedKey
                .ToString()
                .ToLowerInvariant();
            
            if (normalizedKeyLowercase[..5] is "romfs" or "exefs" || normalizedKeyLowercase[..6] is "cheats" or "extras") {
                root = null;
                return true;
            }
            
            if (entry.IsDirectory) {
                switch (Path.GetFileName(normalizedKeyLowercase)) {
                    case "romfs" or "exefs":
                        root = normalizedKey[..^5].ToString();
                        return true;
                    case "cheats" or "extras":
                        root = normalizedKey[..^6].ToString();
                        return true;
                }
                
                continue;
            }

            if (normalizedKeyLowercase.IndexOfAny(_validFoldersSearchValues) is var index && index is -1) {
                continue;
            }

            root = normalizedKey[..index].ToString();
            return true;
        }

        root = null;
        return false;
    }
}