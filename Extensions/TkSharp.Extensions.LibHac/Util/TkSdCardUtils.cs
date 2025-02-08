using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Extensions.LibHac.IO;
using TkSharp.Extensions.LibHac.Models;

namespace TkSharp.Extensions.LibHac.Util;

internal static class TkSdCardUtils
{
    public static bool CheckSdCard(KeySet keys, string sdCardFolderPath, out bool hasUpdate) => CheckSdCard(keys, sdCardFolderPath, out hasUpdate, switchFsContainer: null);

    public static bool CheckSdCard(KeySet keys, string sdCardFolderPath, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        TkLog.Instance.LogDebug("[ROM *] [SD Card] Checking system");
        bool result = CheckSwitchFolder(keys, sdCardFolderPath, out hasUpdate, switchFsContainer);

        string emummcConfig = Path.Combine(sdCardFolderPath, "emuMMC", "emummc.ini");
        if (!File.Exists(emummcConfig)) {
            goto CheckDumps;
        }

        string emummcNintendoPath;

        using (FileStream fs = File.OpenRead(emummcConfig)) {
            using StreamReader reader = new(fs);
            while (reader.ReadLine() is string line) {
                ReadOnlySpan<char> lineContents = line.AsSpan();
                if (lineContents.Length < 15) {
                    continue;
                }

                if (lineContents[..13] is not "nintendo_path" || lineContents[14] is not '=') {
                    continue;
                }

                emummcNintendoPath = Path.Combine(sdCardFolderPath, line[15..]);
                if (!Directory.Exists(emummcNintendoPath)) {
                    break;
                }

                goto ProcessEmummc;
            }
        }

        goto CheckDumps;

    ProcessEmummc:
        TkLog.Instance.LogDebug("[ROM *] [SD Card] Checking EmuMMC.");
        bool emummcResult = CheckSwitchFolder(keys, emummcNintendoPath, out bool emummcHasUpdate, switchFsContainer) && hasUpdate;
        if (!result) result = emummcResult;
        if (!hasUpdate) hasUpdate = emummcHasUpdate;

    CheckDumps:
        TkLog.Instance.LogDebug("[ROM *] [SD Card] Checking legacy dump folder.");
        string legacyNxDumpToolFolder = Path.Combine(sdCardFolderPath, "switch", "nxdumptool");
        bool legacyDumpResult = CheckForDumps(keys, legacyNxDumpToolFolder, out bool legacyDumpHasUpdate, switchFsContainer);
        if (!result) result = legacyDumpResult;
        if (!hasUpdate) hasUpdate = legacyDumpHasUpdate;

        TkLog.Instance.LogDebug("[ROM *] [SD Card] Checking new dump folder.");
        string nxDumpToolFolder = Path.Combine(sdCardFolderPath, "nxdt_rw_poc");
        bool dumpResult = CheckForDumps(keys, nxDumpToolFolder, out bool dumpHasUpdate, switchFsContainer);
        if (!result) result = dumpResult;
        if (!hasUpdate) hasUpdate = dumpHasUpdate;

        return result;
    }

    private static bool CheckSwitchFolder(KeySet keys, string target, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        FatFileSystem.Create(out FatFileSystem? fatFileSystem, target)
            .ThrowIfFailure();
        UniqueRef<IAttributeFileSystem> fs = new(fatFileSystem);

        SwitchFs switchFs = SwitchFs.OpenSdCard(keys, ref fs);
        bool result = TkGameRomUtils.IsValid(switchFs, out hasUpdate);

        if (switchFsContainer is not null) {
            switchFsContainer.CleanupLater(fatFileSystem);
            switchFsContainer.Add((target, switchFs));
            return result;
        }

        fatFileSystem.Dispose();
        switchFs.Dispose();
        return result;
    }

    private static bool CheckForDumps(KeySet keys, string dumpFolder, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        bool hasBaseGame = false;
        hasUpdate = false;

        if (!Directory.Exists(dumpFolder)) {
            hasUpdate = false;
            return false;
        }

        foreach (string file in Directory.EnumerateFiles(dumpFolder, "*.*", SearchOption.AllDirectories)) {
            if (Path.GetExtension(file.AsSpan()) is not (".nsp" or ".xci")) {
                continue;
            }

            hasBaseGame = TkGameRomUtils.IsFileValid(keys, file, out hasUpdate, switchFsContainer);
            if (hasBaseGame && hasUpdate) return true;
        }

        foreach (string file in Directory.EnumerateDirectories(dumpFolder, "*.*", SearchOption.AllDirectories)) {
            if (Path.GetExtension(file.AsSpan()) is not (".nsp" or ".xci")) {
                continue;
            }

            hasBaseGame = TkGameRomUtils.IsSplitFileValid(keys, file, out hasUpdate, switchFsContainer);
            if (hasBaseGame && hasUpdate) return true;
        }

        return hasBaseGame;
    }
}