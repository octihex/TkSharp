using LanguageExt;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.Mergers;
using TkSharp.Merging.ResourceSizeTable;
using MergeTarget = (TkSharp.Core.Models.TkChangelogEntry Changelog, LanguageExt.Either<(TkSharp.Merging.ITkMerger Merger, System.IO.Stream[] Streams), System.IO.Stream> Target);

namespace TkSharp.Merging;

public sealed class TkMerger(ITkModWriter output, ITkRom rom, string locale = "USen")
{
    private readonly ITkModWriter _output = output;
    private readonly ITkRom _rom = rom;
    private readonly string _locale = locale;
    private readonly TkResourceSizeCollector _resourceSizeCollector = new(output, rom);

    public async ValueTask MergeAsync(IEnumerable<TkChangelog> changelogs, CancellationToken ct = default)
    {
        TkChangelog[] tkChangelogs =
            changelogs as TkChangelog[] ?? changelogs.ToArray();

        Task[] tasks = [
            Task.Run(() => MergeIps(tkChangelogs), ct),
            Task.Run(() => MergeSubSdk(tkChangelogs), ct),
            Task.Run(() => CopyCheats(tkChangelogs), ct),
            Task.Run(() => MergeMals(tkChangelogs), ct),
            Task.Run(() => MergeMals(tkChangelogs), ct),
            .. GetTargets(tkChangelogs)
                .Select(entry => Task.Run(() => MergeTarget(entry.Changelog, entry.Target), ct))
        ];
        
        await Task.WhenAll(tasks);
    }

    public void Merge(IEnumerable<TkChangelog> changelogs)
    {
        TkChangelog[] tkChangelogs =
            changelogs as TkChangelog[] ?? changelogs.ToArray();

        MergeIps(tkChangelogs);

        MergeSubSdk(tkChangelogs);

        CopyCheats(tkChangelogs);

        MergeMals(tkChangelogs);

        foreach ((TkChangelogEntry changelog, Either<(ITkMerger, Stream[]), Stream> target) in GetTargets(tkChangelogs)) {
            MergeTarget(changelog, target);
        }
    }

    public void MergeTarget(TkChangelogEntry changelog, Either<(ITkMerger, Stream[]), Stream> target)
    {
        using Stream output = _output.OpenWrite(changelog.Canonical); // TODO: transpose to real output file
        switch (target.Case) {
            case (ITkMerger merger, Stream[] streams): {
                using RentedBuffers<byte> inputs = RentedBuffers<byte>.Allocate(streams); 
                using RentedBuffer<byte> vanilla = _rom.GetVanilla(changelog.Canonical, changelog.Attributes);
                merger.Merge(inputs, vanilla.Segment, output, _resourceSizeCollector);
                break;
            }
            case Stream copy:
                copy.CopyTo(output);
                return;
        }
    }

    private void MergeIps(TkChangelog[] changelogs)
    {
        IEnumerable<TkPatch> versionMatchedPatchFiles = changelogs
            .SelectMany(entry => entry.PatchFiles
                .Where(patch => patch.NsoBinaryId == _rom.NsoBinaryId));

        TkPatch merged = new(_rom.NsoBinaryId);
        foreach (TkPatch patch in versionMatchedPatchFiles) {
            foreach ((uint key, uint value) in patch.Entries) {
                merged.Entries[key] = value;
            }
        }

        using Stream output = _output.OpenWrite($"exefs/{_rom.NsoBinaryId}.ips");
        merged.WriteIps(output);
    }

    private void MergeSubSdk(TkChangelog[] changelogs)
    {
        int index = 0;

        foreach (TkChangelog changelog in changelogs.Reverse()) {
            if (changelog.Source is null) {
                // TODO: Log: source is un-initialized
                continue;
            }

            foreach (string subSdkFile in changelog.SubSdkFiles) {
                if (index > 9) {
                    // Track skipped files
                    index++;
                    continue;
                }

                using Stream input = changelog.Source.OpenRead($"exefs/{subSdkFile}");
                using Stream output = _output.OpenWrite($"exefs/subsdk{++index}");
                output.CopyTo(input);
            }
        }

        if (index > 9) {
            // TODO: Tell user how many subsdk files were skipped
        }
    }

    private void CopyCheats(TkChangelog[] changelogs)
    {
        foreach (TkChangelog changelog in changelogs) {
            if (changelog.Source is null) {
                // TODO: Log: source is un-initialized
                continue;
            }

            foreach (string inputOutput in changelog.CheatFiles.Select(cheatFile => $"cheats/{cheatFile}")) {
                using Stream input = changelog.Source.OpenRead(inputOutput);
                using Stream output = _output.OpenWrite(inputOutput);
                output.CopyTo(input);
            }
        }
    }

    private void MergeMals(TkChangelog[] changelogs)
    {
        using RentedBuffers<byte> combinedBuffers = RentedBuffers<byte>.Allocate(
            changelogs
                .SelectMals(_locale)
                .Select(entry => entry.Changelog.Source!.OpenRead($"romfs/{entry.MalsFile}"))
                .ToArray());

        using RentedBuffer<byte> vanilla = _rom.GetVanilla($"Mals/{_locale}.Product.sarc",
            TkFileAttributes.HasZsExtension | TkFileAttributes.IsProductFile);

        SarcMerger.Instance.Merge(combinedBuffers, vanilla.Segment,
            _output.OpenWrite($"romfs/Mals/{_locale}.Product.sarc.zs"), _resourceSizeCollector);
    }

    private static IEnumerable<MergeTarget> GetTargets(TkChangelog[] changelogs)
    {
        return changelogs
            .SelectMany(
                changelog => changelog.ChangelogFiles
                    .Select(entry => (Entry: entry, Changelog: changelog))
            )
            .GroupBy(
                tuple => tuple.Entry,
                tuple => tuple.Changelog
            )
            .Select(GetInputs);
    }

    private static MergeTarget GetInputs(IGrouping<TkChangelogEntry, TkChangelog> group)
    {
        if (GetMerger(group.Key.Canonical) is ITkMerger merger) {
            return (
                Changelog: group.Key,
                Target: (Merger: merger,
                    Streams: group
                        .Select(changelog => changelog.Source!.OpenRead(group.Key.Canonical))
                        .ToArray()
                )
            );
        }

        return (
            Changelog: group.Key,
            Target: group.Last().Source!.OpenRead(group.Key.Canonical)
        );
    }

    public static ITkMerger? GetMerger(ReadOnlySpan<char> canonical)
    {
        return canonical switch {
            "GameData/GameDataList.byml" => GameDataMerger.Instance,
            "RSDB/Tag.Product.rstbl.byml" => RsdbTagMerger.Instance,
            "RSDB/RumbleCall.Product.rstbl.byml" or "RSDB/UIScreen.Product.rstbl.byml" => RsdbRowMerger.Name,
            "RSDB/TagDef.Product.rstbl.byml" => RsdbRowMerger.FullTagId,
            "RSDB/ActorInfo.Product.rstbl.byml" or
                "RSDB/AttachmentActorInfo.Product.rstbl.byml" or
                "RSDB/Challenge.Product.rstbl.byml" or
                "RSDB/EnhancementMaterialInfo.Product.rstbl.byml" or
                "RSDB/EventPlayEnvSetting.Product.rstbl.byml" or
                "RSDB/EventSetting.Product.rstbl.byml" or
                "RSDB/GameActorInfo.Product.rstbl.byml" or
                "RSDB/GameAnalyzedEventInfo.Product.rstbl.byml" or
                "RSDB/GameEventBaseSetting.Product.rstbl.byml" or
                "RSDB/GameEventMetadata.Product.rstbl.byml" or
                "RSDB/LoadingTips.Product.rstbl.byml" or
                "RSDB/Location.Product.rstbl.byml" or
                "RSDB/LocatorData.Product.rstbl.byml" or
                "RSDB/PouchActorInfo.Product.rstbl.byml" or
                "RSDB/XLinkPropertyTable.Product.rstbl.byml" or
                "RSDB/XLinkPropertyTableList.Product.rstbl.byml" => RsdbRowMerger.RowId,
            _ => Path.GetExtension(canonical) switch {
                ".bfarc" or ".bkres" or ".blarc" or ".genvb" or ".pack" or ".ta" => SarcMerger.Instance,
                ".byml" or ".bgyml" => BymlMerger.Instance,
                ".msbt" => MsbtMerger.Instance,
                _ => null
            }
        };
    }
}