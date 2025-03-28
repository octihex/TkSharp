using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using TkSharp.Extensions.LibHac.Models;

namespace TkSharp.Extensions.LibHac.Util;

public static class TkNandUtils
{
    public static bool IsValid(KeySet keys, string nandFolderPath, out bool hasUpdate)
        => IsValid(keys, nandFolderPath, out hasUpdate, switchFsContainer: null);

    internal static bool IsValid(KeySet keys, string nandFolderPath, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        string systemContents = Path.Combine(nandFolderPath, "user", "Contents", "registered");
        if (!Directory.Exists(systemContents)) {
            hasUpdate = false;
            return false;
        }

        List<IFileSystem> sources = Directory.EnumerateDirectories(systemContents).Select(static IFileSystem (folder) => {
            LocalFileSystem.Create(out LocalFileSystem? localFileSystem, folder)
                .ThrowIfFailure();
            return localFileSystem;
        }).ToList();
        
        LayeredFileSystem fs = new(sources);

        SwitchFs switchFs = SwitchFs.OpenNcaDirectory(keys, fs);
        bool result = TkGameRomUtils.IsValid(switchFs, out hasUpdate);

        if (switchFsContainer is not null) {
            foreach (IFileSystem disposable in sources) {
                switchFsContainer.CleanupLater(disposable);
            }
            
            switchFsContainer.Add((nandFolderPath, switchFs));
            return result;
        }

        foreach (IFileSystem disposable in sources) {
            disposable.Dispose();
        }
        
        fs.Dispose();
        switchFs.Dispose();
        return result;
    }
}