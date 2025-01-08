using LibHac;
using LibHac.Fs;
using LibHac.Fs.Fsa;

namespace TkSharp.Extensions.LibHac.Common;

public class FatArchiveDirectory(IDirectory baseDirectory) : IDirectory
{
    private readonly IDirectory _baseDirectory = baseDirectory;

    protected override Result DoRead(out long entriesRead, Span<DirectoryEntry> entryBuffer)
    {
        Result result = _baseDirectory.Read(out entriesRead, entryBuffer);

        for (int i = 0; i < entriesRead; i++) {
            ref DirectoryEntry entry = ref entryBuffer[i];
            ReadOnlySpan<byte> fileName = entry.Name.ItemsRo;
            if (fileName.LastIndexOf((byte)'.') is var extSeparatorIndex and > -1) {
                if (fileName[extSeparatorIndex..(extSeparatorIndex + 4)].SequenceEqual(".nca"u8)) {
                    entry.Attributes |= NxFileAttributes.Archive;
                }
            }
        }

        return result;
    }

    protected override Result DoGetEntryCount(out long entryCount)
    {
        return _baseDirectory.GetEntryCount(out entryCount);
    }
}