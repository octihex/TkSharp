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
        catch (ArgumentException) {
            rom = null;
            return false;
        }
        catch (Exception ex) {
            TkLog.Instance.LogError(ex, "Unexpected error when retrieving the configured TotK rom (TkRom).");
            rom = null;
            return false;
        }
    }
    
    public ITkRom GetRom()
    {
        if (_config.ExtractedGameDumpFolderPath.Get(out string? extractedGameDumpPath)) {
            return new ExtractedTkRom(extractedGameDumpPath, _checksums);
        }

        if (TryGetKeys() is not KeySet keys) {
            throw new ArgumentException("The TotK configuration could not be read because no prod.keys file is configured.");
        }

        // Track a list of SwitchFs instances in use
        // and dispose with the ITkRom 
        SwitchFsContainer collected = [];

        if (TryBuild(_config.PackagedBaseGame, keys, collected) is { } buildAfterBaseGame) {
            return buildAfterBaseGame;
        }

        if (TryBuild(_config.PackagedUpdate, keys, collected) is { } buildAfterUpdate) {
            return buildAfterUpdate;
        }

        if (TryBuild(_config.SdCard, keys, collected) is { } buildSdCard) {
            return buildSdCard;
        }
        
        throw new ArgumentException("Invalid or incomplete TotK configuration.");
    }

    private KeySet? TryGetKeys()
    {
        if (_config.KeysFolder.Get(out string? keysFolder)) {
            return TkKeyUtils.GetKeysFromFolder(keysFolder);
        }

        TkKeyUtils.TryGetKeys(out KeySet? keys);
        return keys;
    }

    private TkSwitchRom? TryBuild<T>(in TkExtensibleConfig<T> config, KeySet keys, SwitchFsContainer collected)
    {
        if (!config.Get(out _, keys, collected)) {
            return null;
        }

        Title? main = null;
        Title? update = null;

        foreach (SwitchFs switchFs in collected) {
            if (!switchFs.Applications.TryGetValue(TkGameRomUtils.EX_KING_APP_ID, out Application? totk)) {
                continue;
            }

            if (totk.Main is not null) {
                main = totk.Main;
            }

            if (totk.Patch is not null) {
                update = totk.Patch;
            }

            if (main is not null && update is not null) {
                goto IsValid;
            }
        }

        return null;
    
    IsValid:
        IFileSystem fs = main.MainNca.Nca
            .OpenFileSystemWithPatch(update.MainNca.Nca, NcaSectionType.Data, IntegrityCheckLevel.ErrorOnInvalid);
        return new TkSwitchRom(fs, collected, _checksums);
    }
}