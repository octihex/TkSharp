using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using TkSharp.Extensions.LibHac.Extensions;
using TkSharp.Extensions.LibHac.Models;

namespace TkSharp.Extensions.LibHac.Util;

public static class TkGameRomUtils
{
    public const ulong EX_KING_APP_ID = 0x0100F2C0115B6000;
    
    public static bool IsValid(KeySet keys, string target, out bool hasUpdate)
    {
        bool result = IsValid(keys, target, out hasUpdate, switchFsContainer: null);
        return result;
    }

    internal static bool IsValid(KeySet keys, string target, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        if (File.Exists(target)) {
            return IsFileValid(keys, target, out hasUpdate, switchFsContainer);
        }

        if (Directory.Exists(target)) {
            IsSplitFileValid(keys, target, out hasUpdate, switchFsContainer);
        }

        hasUpdate = false;
        return false;
    }

    public static bool IsFileValid(KeySet keys, string target, out bool hasUpdate)
    {
        bool result = IsFileValid(keys, target, out hasUpdate, switchFsContainer: null);
        return result;
    }

    internal static bool IsFileValid(KeySet keys, string target, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        if (!File.Exists(target)) {
            hasUpdate = false;
            return false;
        }

        LocalStorage storage = new(target, FileAccess.Read);
        SwitchFs nx = storage.GetSwitchFs(target, keys);
        bool result = IsValid(nx, out hasUpdate);

        if (switchFsContainer is null) {
            nx.Dispose();
            storage.Dispose();
            return result;
        }

        switchFsContainer.CleanupLater(storage);
        switchFsContainer.Add((target, nx));
        return result;
    }

    public static bool IsSplitFileValid(KeySet keys, string target, out bool hasUpdate)
    {
        bool result = IsSplitFileValid(keys, target, out hasUpdate, switchFsContainer: null);
        return result;
    }

    internal static bool IsSplitFileValid(KeySet keys, string target, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        if (!Directory.Exists(target)) {
            hasUpdate = false;
            return false;
        }

        IList<IStorage> splitFiles = [
            .. Directory.EnumerateFiles(target)
                .OrderBy(f => f)
                .Select(f => new LocalStorage(f, FileAccess.Read))
        ];

        ConcatenationStorage storage = new(splitFiles, true);
        SwitchFs nx = storage.GetSwitchFs(target, keys);
        bool result = IsValid(nx, out hasUpdate);
        
        if (switchFsContainer is null) {
            nx.Dispose();
            storage.Dispose();
            return result;
        }

        switchFsContainer.CleanupLater(storage);
        switchFsContainer.Add(($"{target} (Split File)", nx));
        return result;
    }

    public static bool IsValid(SwitchFs nx, out bool hasUpdate)
    {
        if (!nx.Applications.TryGetValue(EX_KING_APP_ID, out Application? totk)) {
            hasUpdate = false;
            return false;
        }

        bool result = totk.Main is not null;
        hasUpdate = totk.DisplayVersion is not "1.0.0";
        return result;
    }
}