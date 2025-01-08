using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using Path = LibHac.Fs.Path;

namespace TkSharp.Extensions.LibHac.Common;

public class ArchiveFileSystem : IAttributeFileSystem
{
    private readonly IAttributeFileSystem _baseFileSystem;

    public ArchiveFileSystem(IAttributeFileSystem baseFileSystem)
    {
        _baseFileSystem = baseFileSystem;
    }

    protected override Result DoCreateDirectory(in Path path, NxFileAttributes archiveAttribute)
    {
        return _baseFileSystem.CreateDirectory(in path);
    }

    protected override Result DoCreateDirectory(in Path path)
    {
        return _baseFileSystem.CreateDirectory(in path);
    }

    protected override Result DoGetFileAttributes(out NxFileAttributes attributes, in Path path)
    {
        string pathString = path.ToString();

        Result res = _baseFileSystem.GetEntryType(out DirectoryEntryType entryType, in path);
        if (res.IsSuccess() && entryType == DirectoryEntryType.Directory && pathString.EndsWith(".nca"))
        {
            attributes = NxFileAttributes.Directory | NxFileAttributes.Archive;
        }
        else if (pathString.Contains("private") || IsInNcaFolder(pathString) || pathString.EndsWith(".nca"))
        {
            attributes = NxFileAttributes.Archive;
        }
        else
        {
            attributes = NxFileAttributes.None;
        }

        return Result.Success;
    }

    protected override Result DoSetFileAttributes(in Path path, NxFileAttributes attributes)
    {
        return _baseFileSystem.SetFileAttributes(in path, attributes);
    }

    protected override Result DoGetFileSize(out long fileSize, in Path path)
    {
        return _baseFileSystem.GetFileSize(out fileSize, in path);
    }

    protected override Result DoCreateFile(in Path path, long size, CreateFileOptions options)
    {
        return _baseFileSystem.CreateFile(in path, size, options);
    }

    protected override Result DoDeleteFile(in Path path)
    {
        return _baseFileSystem.DeleteFile(in path);
    }

    protected override Result DoDeleteDirectory(in Path path)
    {
        return _baseFileSystem.DeleteDirectory(in path);
    }

    protected override Result DoDeleteDirectoryRecursively(in Path path)
    {
        return _baseFileSystem.DeleteDirectoryRecursively(in path);
    }

    protected override Result DoCleanDirectoryRecursively(in Path path)
    {
        return _baseFileSystem.CleanDirectoryRecursively(in path);
    }

    protected override Result DoRenameFile(in Path srcPath, in Path dstPath)
    {
        return _baseFileSystem.RenameFile(in srcPath, in dstPath);
    }

    protected override Result DoRenameDirectory(in Path srcPath, in Path dstPath)
    {
        return _baseFileSystem.RenameDirectory(in srcPath, in dstPath);
    }

    protected override Result DoGetEntryType(out DirectoryEntryType entryType, in Path path)
    {
        return _baseFileSystem.GetEntryType(out entryType, in path);
    }

    protected override Result DoOpenFile(ref UniqueRef<IFile> outFile, in Path path, OpenMode mode)
    {
        return _baseFileSystem.OpenFile(ref outFile, in path, mode);
    }

    protected override Result DoOpenDirectory(ref UniqueRef<IDirectory> outDirectory, in Path path, OpenDirectoryMode mode)
    {
        return _baseFileSystem.OpenDirectory(ref outDirectory, in path, mode);
    }

    protected override Result DoCommit()
    {
        return _baseFileSystem.Commit();
    }

    private bool IsInNcaFolder(string path)
    {
        var segments = path.Split('/');
        return segments.Any(segment => segment.EndsWith(".nca"));
    }
}