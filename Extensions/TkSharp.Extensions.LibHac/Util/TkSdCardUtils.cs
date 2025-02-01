using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using TkSharp.Extensions.LibHac.IO;
using TkSharp.Extensions.LibHac.Models;

namespace TkSharp.Extensions.LibHac.Util;

internal static class TkSdCardUtils
{
    public static bool CheckSdCard(KeySet keys, string sdCardFolderPath, out bool hasUpdate) => CheckSdCard(keys, sdCardFolderPath, out hasUpdate, switchFsContainer: null);

    public static bool CheckSdCard(KeySet keys, string sdCardFolderPath, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        if (CheckSwitchFolder(keys, sdCardFolderPath, out hasUpdate, switchFsContainer) && hasUpdate) {
            return true;
        }

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
        return CheckSwitchFolder(keys, emummcNintendoPath, out hasUpdate, switchFsContainer) && hasUpdate;

    CheckDumps:
        string legacyNxDumpToolFolder = Path.Combine(sdCardFolderPath, "switch", "nxdumptool");
        if (CheckForDumps(keys, legacyNxDumpToolFolder, out hasUpdate, switchFsContainer) && hasUpdate) {
            return true;
        }

        string nxDumpToolFolder = Path.Combine(sdCardFolderPath, "nxdt_rw_poc");
        return CheckForDumps(keys, nxDumpToolFolder, out hasUpdate, switchFsContainer) && hasUpdate;
    }

    private static bool CheckSwitchFolder(KeySet keys, string target, out bool hasUpdate, SwitchFsContainer? switchFsContainer)
    {
        FatFileSystem.Create(out FatFileSystem? fatFileSystem, target)
            .ThrowIfFailure();
        using FatFileSystem localFs = fatFileSystem;
        UniqueRef<IAttributeFileSystem> fs = new(localFs);

        SwitchFs switchFs = SwitchFs.OpenSdCard(keys, ref fs);
        if (TkGameRomUtils.IsValid(switchFs, out hasUpdate)) {
            switchFsContainer?.Add((target, switchFs));
            return true;
        }

        return false;
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