using System.Diagnostics.Contracts;
using LibHac.Common.Keys;
using TkSharp.Core;
using TkSharp.Extensions.LibHac.Util;

namespace TkSharp.Extensions.LibHac;

public class TkExtensibleRomProviderBuilder
{
    private readonly TkChecksums _checksums;
    private TkExtensibleRomConfig _root = new();

    private TkExtensibleRomProviderBuilder(TkChecksums checksums)
    {
        _checksums = checksums;
    }
    
    public static TkExtensibleRomProviderBuilder Create(Stream checksums)
    {
        return new TkExtensibleRomProviderBuilder(TkChecksums.FromStream(checksums));
    }
    
    public static TkExtensibleRomProviderBuilder Create(TkChecksums checksums)
    {
        return new TkExtensibleRomProviderBuilder(checksums);
    }
    
    public TkExtensibleRomProvider Build()
    {
        return new TkExtensibleRomProvider(_root, _checksums);
    }

    public TkExtensibleRomProviderBuilder WithKeysFolder(string? keysFolderPath)
    {
        _root.KeysFolder.Set(() => keysFolderPath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithKeysFolder(Func<string?> keysFolderPath)
    {
        _root.KeysFolder.Set(keysFolderPath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithExtractedGameDump(string? gameDumpPath)
    {
        _root.ExtractedGameDumpFolderPath.Set(() => gameDumpPath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithExtractedGameDump(Func<string?> gameDumpPath)
    {
        _root.ExtractedGameDumpFolderPath.Set(gameDumpPath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithSdCard(string? sdCardFolderPath)
    {
        _root.SdCard.Set(() => sdCardFolderPath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithSdCard(Func<string?> sdCardFolderPath)
    {
        _root.SdCard.Set(sdCardFolderPath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithPackagedBaseGame(string? packagedBaseGamePath)
    {
        _root.PackagedBaseGame.Set(() => packagedBaseGamePath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithPackagedBaseGame(Func<string?> packagedBaseGamePath)
    {
        _root.PackagedBaseGame.Set(packagedBaseGamePath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithPackagedUpdate(string? packagedUpdatePath)
    {
        _root.PackagedUpdate.Set(() => packagedUpdatePath);
        return this;
    }

    public TkExtensibleRomProviderBuilder WithPackagedUpdate(Func<string?> packagedUpdatePath)
    {
        _root.PackagedUpdate.Set(packagedUpdatePath);
        return this;
    }

    /// <summary>
    /// Get a report on the current state of the builder.
    /// </summary>
    /// <returns></returns>
    [Pure]
    public TkExtensibleRomReport GetReport()
    {
        var report = TkExtensibleRomReportBuilder.Create();

        if (_root.ExtractedGameDumpFolderPath.Get(out string? gameDumpPath)) {
            const string infoKey = "Game Dump Path";
            bool hasBaseGame = TkGameDumpUtils.CheckGameDump(gameDumpPath, out bool hasUpdate);
            report.SetHasBaseGame(hasBaseGame, infoKey);
            report.SetHasUpdate(hasUpdate, infoKey);
        }

        if (!TkKeyUtils.TryGetKeys(out KeySet? keys)) {
            // Without keys, further analysis is impossible
            goto Result;
        }
        
        report.SetKeys(keys);

        if (_root.SdCard.Get(out string? sdCardFolderPath)) {
            const string infoKey = "Installed or Dumped on SD Card";
            bool hasBaseGameInSdCard = TkSdCardUtils.CheckSdCard(keys, sdCardFolderPath, out bool hasUpdateInSdCard);
            report.SetHasBaseGame(hasBaseGameInSdCard, infoKey);
            report.SetHasUpdate(hasUpdateInSdCard, infoKey);
        }

        if (_root.PackagedBaseGame.Get(out string? packagedBaseGamePath)) {
            const string packagedInfoKey = "Packaged Base Game File";
            bool hasBaseGameAsFile = TkGameRomUtils.IsFileValid(keys, packagedBaseGamePath, out bool hasUpdateAsFile);
            report.SetHasBaseGame(hasBaseGameAsFile, packagedInfoKey);
            report.SetHasUpdate(hasUpdateAsFile, packagedInfoKey);
            
            const string splitFileInfoKey = "Packaged Base Game Split File";
            bool hasBaseGameAsSplitFile = TkGameRomUtils.IsSplitFileValid(keys, packagedBaseGamePath, out bool hasUpdateAsSplitFile);
            report.SetHasBaseGame(hasBaseGameAsSplitFile, splitFileInfoKey);
            report.SetHasUpdate(hasUpdateAsSplitFile, splitFileInfoKey);
        }

        if (_root.PackagedUpdate.Get(out string? packagedUpdatePath)) {
            const string packagedInfoKey = "Packaged Update File";
            bool hasBaseGameAsFile = TkGameRomUtils.IsFileValid(keys, packagedUpdatePath, out bool hasUpdateAsFile);
            report.SetHasBaseGame(hasBaseGameAsFile, packagedInfoKey);
            report.SetHasUpdate(hasUpdateAsFile, packagedInfoKey);
            
            const string splitFileInfoKey = "Packaged Update Split File";
            bool hasBaseGameAsSplitFile = TkGameRomUtils.IsSplitFileValid(keys, packagedUpdatePath, out bool hasUpdateAsSplitFile);
            report.SetHasBaseGame(hasBaseGameAsSplitFile, splitFileInfoKey);
            report.SetHasUpdate(hasUpdateAsSplitFile, splitFileInfoKey);
        }

    Result:
        return report.Build();
    }
}