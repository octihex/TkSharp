using System.Diagnostics.CodeAnalysis;
using LibHac.Common.Keys;
using LibHac.Fs.Fsa;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Microsoft.Extensions.Logging;
using TkSharp.Core;
using TkSharp.Core.IO;
using TkSharp.Extensions.LibHac.IO;
using TkSharp.Extensions.LibHac.Models;
using TkSharp.Extensions.LibHac.Util;

namespace TkSharp.Extensions.LibHac;

public class TkExtensibleRomProvider : ITkRomProvider
{
    private readonly TkExtensibleRomConfig _config;
    private readonly TkChecksums _checksums;

    internal TkExtensibleRomProvider(TkExtensibleRomConfig config, TkChecksums checksums)
    {
        _config = config;
        _checksums = checksums;
    }

    public bool TryGetRom([MaybeNullWhen(false)] out ITkRom rom)
    {
        try {
            rom = GetRom();
            return true;
        }
        catch (Exception ex) {
            TkLog.Instance.LogDebug(ex, "Unexpected error when retrieving the configured TotK rom (TkRom).");
            rom = null;
            return false;
        }
    }

    public ITkRom GetRom()
    {
        _ = _config.PreferredVersion.Get(out string? preferredVersion);

        TkLog.Instance.LogDebug("[ROM *] Checking Extracted Game Dump");
        if (_config.ExtractedGameDumpFolderPath.Get(out IEnumerable<string>? extractedGameDumpPaths)
            && GetPreferred(extractedGameDumpPaths, preferredVersion) is string extractedGameDumpPath) {
            return new ExtractedTkRom(extractedGameDumpPath, _checksums);
        }

        TkLog.Instance.LogDebug("[ROM *] Looking for Keys");
        if (TryGetKeys() is not KeySet keys) {
            throw new ArgumentException("The TotK configuration could not be read because no prod.keys file is configured.");
        }

        // Track a list of SwitchFs instances in use
        // and dispose with the ITkRom 
        SwitchFsContainer collected = [];
        Title? main = null, update = null, alternateUpdate = null;

        TkLog.Instance.LogDebug("[ROM *] Checking Packaged Base Game");
        if (TryBuild(_config.PackagedBaseGame, keys, collected, preferredVersion, ref main, ref update, ref alternateUpdate) is { } buildAfterBaseGame) {
            return buildAfterBaseGame;
        }

        TkLog.Instance.LogDebug("[ROM *] Checking Packaged Update");
        if (TryBuild(_config.PackagedUpdate, keys, collected, preferredVersion, ref main, ref update, ref alternateUpdate) is { } buildAfterUpdate) {
            return buildAfterUpdate;
        }

        TkLog.Instance.LogDebug("[ROM *] Checking SD Card");
        if (TryBuild(_config.SdCard, keys, collected, preferredVersion, ref main, ref update, ref alternateUpdate) is { } buildSdCard) {
            return buildSdCard;
        }

        if (main is not null && (update is not null || alternateUpdate is not null)) {
            if (update is null) {
                TkLog.Instance.LogWarning(
                    "[ROM *] The configured preferred version ({Version}) could not be found",
                    preferredVersion);
            }

            TkLog.Instance.LogDebug("[ROM *] Configuration Valid (Mixed)");
            IFileSystem fs = main.MainNca.Nca
                .OpenFileSystemWithPatch((update ?? alternateUpdate)!.MainNca.Nca,
                    NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
            return new TkSwitchRom(fs, collected.AsFsList(), _checksums);
        }

        throw new ArgumentException("Invalid or incomplete TotK configuration.");
    }

    private KeySet? TryGetKeys()
    {
        if (_config.KeysFolder.Get(out string? keysFolder)) {
            TkLog.Instance.LogDebug("[ROM *] Looking for Keys in {KeysFolder}", keysFolder);
            return TkKeyUtils.GetKeysFromFolder(keysFolder);
        }

        TkLog.Instance.LogDebug("[ROM *] Looking for roaming keys");
        TkKeyUtils.TryGetKeys(out KeySet? keys);
        return keys;
    }

    private TkSwitchRom? TryBuild<T>(in TkExtensibleConfig<T> config, KeySet keys, SwitchFsContainer collected,
        string? preferredVersion, ref Title? main, ref Title? update, ref Title? alternateUpdate)
    {
        if (!config.Get(out _, keys, collected)) {
            return null;
        }

        foreach ((string label, SwitchFs switchFs) in collected) {
            if (!switchFs.Applications.TryGetValue(TkGameRomUtils.EX_KING_APP_ID, out Application? totk)) {
                TkLog.Instance.LogDebug("[ROM *] TotK missing in {Label}", label);
                continue;
            }

            if (totk.Main is not null) {
                TkLog.Instance.LogDebug("[ROM *] Base Game found in {Label}", label);
                main = totk.Main;
            }

            if (totk.Patch is not null) {
                if (preferredVersion is not null && preferredVersion != totk.DisplayVersion) {
                    TkLog.Instance.LogDebug("[ROM *] Update {Version} found in {Label} but is not preferred.",
                        totk.DisplayVersion, label);
                    alternateUpdate = totk.Patch;
                    continue;
                }

                TkLog.Instance.LogDebug("[ROM *] Update {Version} found in {Label}", totk.DisplayVersion, label);
                update = totk.Patch;
            }

            if (main is not null && update is not null) {
                goto IsValid;
            }
        }

        return null;

    IsValid:
        TkLog.Instance.LogDebug("[ROM *] Configuration Valid");
        IFileSystem fs = main.MainNca.Nca
            .OpenFileSystemWithPatch(update.MainNca.Nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
        return new TkSwitchRom(fs, collected.AsFsList(), _checksums);
    }

    private static string? GetPreferred(IEnumerable<string> extractedGameDumpPaths, string? preferredVersion)
    {
        int? parsedVersion = int.TryParse(preferredVersion?.Replace(".", string.Empty), out int parsedVersionInline)
            ? parsedVersionInline
            : null;

        if (parsedVersion is not int version) {
            return extractedGameDumpPaths
                .FirstOrDefault(path => TkGameDumpUtils.CheckGameDump(path, out bool hasUpdate) && hasUpdate);
        }

        string? result = null;
        foreach (string gameDumpPath in extractedGameDumpPaths) {
            if (!TkGameDumpUtils.CheckGameDump(gameDumpPath, out bool hasUpdate, out int foundVersion) || !hasUpdate) {
                continue;
            }
            
            result = gameDumpPath;

            if (foundVersion == version) {
                return gameDumpPath;
            }
        }

        return result;
    }
}