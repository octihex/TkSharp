using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using TkSharp.Core;
using TkSharp.Core.IO.ModSources;
using TkSharp.Core.Models;
using TkSharp.Merging;

namespace TkSharp.IO.Readers;

public sealed class SevenZipModReader(ITkSystemProvider systemProvider, ITkRomProvider romProvider) : ITkModReader
{
    private readonly ITkSystemProvider _systemProvider = systemProvider;
    private readonly ITkRomProvider _romProvider = romProvider;

    public async ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext context = default, CancellationToken ct = default)
    {
        if (input is not string fileName || stream is null) {
            return null;
        }
        
        using SevenZipArchive archive = SevenZipArchive.Open(stream);
        if (!ArchiveModReader.LocateRoot(archive, out IArchiveEntry? root)) {
            return null;
        }

        context.EnsureId();
        
        ArchiveModSource source = new(archive, root);
        ITkModWriter writer = _systemProvider.GetSystemWriter(context);

        TkChangelogBuilder builder = new(source, writer, _romProvider.GetRom(),
            _systemProvider.GetSystemSource(context.Id.ToString())
        );
        TkChangelog changelog = await builder.BuildAsync(ct);

        return new TkMod {
            Id = context.Id,
            Name = Path.GetFileNameWithoutExtension(fileName),
            Changelog = changelog
        };
    }

    public bool IsKnownInput(object? input)
    {
        return input is string path
               && Path.GetExtension(path.AsSpan()) is ".7z";
    }
}