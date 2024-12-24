using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSrv;
using LibHac.FsSrv.FsCreator;
using LibHac.Spl;
using LibHac.Tools.Es;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using TkSharp.Extensions.LibHac.Common;
using Path = System.IO.Path;

namespace TkSharp.Extensions.LibHac.Extensions;

public static class FileSystemExtensions
{
    public static SwitchFs GetSwitchFs(this IStorage storage, string filePath, KeySet keys, out SharedRef<IFileSystem> outputFs)
    {
        ReadOnlySpan<char> extension = Path.GetExtension(filePath.AsSpan());
        outputFs = default;

        return extension switch {
            ".nsp" => OpenNsp(keys, storage, out outputFs),
            ".xci" => OpenXci(keys, storage),
            _ => throw new ArgumentException($"Unsupported file extension: '{extension}'", nameof(filePath)),
        };
    }

    private static SwitchFs OpenNsp(KeySet keys, IStorage storage, out SharedRef<IFileSystem> outputFs)
    {
        SharedRef<IStorage> storageShared = new(storage);
        outputFs = new SharedRef<IFileSystem>();
        new PartitionFileSystemCreator().Create(ref outputFs, ref storageShared);

        ImportTikFiles(keys, outputFs.Get);
        return SwitchFs.OpenNcaDirectory(keys, outputFs.Get);
    }

    private static SwitchFs OpenXci(KeySet keys, IStorage storage)
    {
        Xci xci = new(keys, storage);
        using XciPartition fs = xci.OpenPartition(XciPartitionType.Secure);
        return SwitchFs.OpenNcaDirectory(keys, fs);
    }

    public static Stream OpenFileStream(this IFileSystem fs, string path)
    {
        UniqueRef<IFile> file = new();
        fs.OpenFile(ref file, path.ToU8Span(), OpenMode.Read).ThrowIfFailure();
        return new NxRefFileStream(file);
    }

    private static void ImportTikFiles(KeySet keys, IFileSystem fs)
    {
        foreach (DirectoryEntryEx? entry in fs.EnumerateEntries("*.tik", SearchOptions.Default)) {
            var file = new UniqueRef<IFile>();
            fs.OpenFile(ref file.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
            using NxFileStream stream = new(file.Get, OpenMode.Read, false);
            ImportTikFile(keys, stream);
        }
    }

    private static void ImportTikFile(KeySet keys, Stream stream)
    {
        Ticket ticket = new(stream);
        ExternalKeySet externalKeySet = keys.ExternalKeySet;
        RightsId rightsId = new(ticket.RightsId);
        AccessKey key = new(ticket.GetTitleKey(keys));
        externalKeySet.Add(rightsId, key);
    }
}