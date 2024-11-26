using System.Runtime.CompilerServices;
using SharpCompress.Archives;

namespace TkSharp.Core.IO.ModSources;

public sealed class ArchiveModSource(IArchive archive, IArchiveEntry rootEntry) : TkModSourceBase<IArchiveEntry>(rootEntry.Key!)
{
    private readonly IArchive _archive = archive;
    private readonly IArchiveEntry _rootEntry = rootEntry;

    protected override IEnumerable<IArchiveEntry> Files => _archive.Entries
        .Where(entry => !entry.IsDirectory && entry.Key?.StartsWith(_rootEntry.Key!) is true);

    protected override Stream OpenRead(IArchiveEntry input)
    {
        lock (_archive) {
            return input.OpenEntryStream();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string GetFileName(IArchiveEntry input)
    {
        return input.Key!;
    }
}