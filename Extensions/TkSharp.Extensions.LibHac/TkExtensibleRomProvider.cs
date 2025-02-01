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
            TkLog.Instance.LogError(ex, "Unexpected error when retrieving the configured TotK rom (TkRom).");
            rom = null;
            return false;
        }
    }
    
    public ITkRom GetRom()
    {
        TkLog.Instance.LogDebug("[ROM *] Checking Extracted Game Dump");
        if (_config.ExtractedGameDumpFolderPath.Get(out string? extractedGameDumpPath)) {
            return new ExtractedTkRom(extractedGameDumpPath, _checksums);
        }

        TkLog.Instance.LogDebug("[ROM *] Looking for Keys");
        if (TryGetKeys() is not KeySet keys) {
            throw new ArgumentException("The TotK configuration could not be read because no prod.keys file is configured.");
        }

        // Track a list of SwitchFs instances in use
        // and dispose with the ITkRom 
        SwitchFsContainer collected = [];
        Title? main = null, update = null;

        TkLog.Instance.LogDebug("[ROM *] Checking Packaged Base Game");
        if (TryBuild(_config.PackagedBaseGame, keys, collected, ref main, ref update) is { } buildAfterBaseGame) {
            return buildAfterBaseGame;
        }

        TkLog.Instance.LogDebug("[ROM *] Checking Packaged Update");
        if (TryBuild(_config.PackagedUpdate, keys, collected, ref main, ref update) is { } buildAfterUpdate) {
            return buildAfterUpdate;
        }

        TkLog.Instance.LogDebug("[ROM *] Checking SD Card");
        if (TryBuild(_config.SdCard, keys, collected, ref main, ref update) is { } buildSdCard) {
            return buildSdCard;
        }

        TkLog.Instance.LogDebug("[ROM *] Configuration Valid (Mixed)");
        if (main is not null && update is not null) {
            IFileSystem fs = main.MainNca.Nca
                .OpenFileSystemWithPatch(update.MainNca.Nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
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

    private TkSwitchRom? TryBuild<T>(in TkExtensibleConfig<T> config, KeySet keys, SwitchFsContainer collected, ref Title? main, ref Title? update)
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
}