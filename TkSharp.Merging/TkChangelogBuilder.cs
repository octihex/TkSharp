using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;
using TkSharp.Merging.ChangelogBuilders;

namespace TkSharp.Merging;

public class TkChangelogBuilder(ITkModSource source, ITkModWriter writer, ITkRom tk)
{
    private readonly ITkModSource _source = source;
    private readonly ITkModWriter _writer = writer;
    private readonly ITkRom _tk = tk;

    private readonly TkChangelog _changelog = new() {
        BuilderVersion = 100,
        GameVersion = tk.GameVersion
    };

    public async ValueTask<TkChangelog> BuildParallel(CancellationToken ct = default)
    {
        await Task.WhenAll(_source.Files.Select(
                file => Task.Run(() => BuildTarget(file.FilePath, file.Entry), ct)
            ))
            .ConfigureAwait(false);

        return _changelog;
    }

    public Task<TkChangelog> BuildAsync(CancellationToken ct = default)
    {
        return Task.Run(Build, ct);
    }
    
    public TkChangelog Build()
    {
        foreach ((string file, object entry) in _source.Files) {
            BuildTarget(file, entry);
        }

        return _changelog;
    }

    private void BuildTarget(string file, object entry)
    {
        TkPath path = TkPath.FromPath(file, _source.PathToRoot, out bool isInvalid);
        if (isInvalid) {
            return;
        }
        
        string canonical = path.Canonical.ToString();

        using Stream content = _source.OpenRead(entry);

        switch (path) {
            case { Root: "exefs", Extension: ".ips" }:
                if (TkPatch.FromIps(content, path.Canonical[..^4].ToString()) is TkPatch patch) {
                    _changelog.PatchFiles.Add(patch);
                }

                return;
            case { Root: "exefs", Extension: ".pchtxt" }:
                if (TkPatch.FromPchTxt(content) is TkPatch patchFromPchtxt) {
                    _changelog.PatchFiles.Add(patchFromPchtxt);
                }

                return;
            case { Root: "exefs", Canonical.Length: 7 } when path.Canonical[..6] is "subsdk":
                _changelog.SubSdkFiles.Add(canonical);
                goto Copy;
            case { Root: "cheats" }:
                _changelog.CheatFiles.Add(canonical);
                goto Copy;
            case { Extension: ".ini" or ".rsizetable" }:
                return;
        }

        goto Build;

    Copy:
        string outputFilePath = Path.Combine(path.Root.ToString(), canonical);
        // ReSharper disable once ConvertToUsingDeclaration
        using (Stream output = _writer.OpenWrite(outputFilePath)) {
            content.CopyTo(output);
        }

        return;

    Build:
        if (GetChangelogBuilder(path) is not ITkChangelogBuilder builder) {
            AddChangelogMetadata(path, canonical, ChangelogEntryType.Copy, zsDictionaryId: -1);
            goto Copy;
        }

        using RentedBuffer<byte> raw = RentedBuffer<byte>.Allocate(content);
        _ = content.Read(raw.Span);
        using RentedBuffer<byte> src = RentedBuffer<byte>.Allocate(TkZstd.GetDecompressedSize(raw.Span));
        _tk.Zstd.Decompress(raw.Span, src.Span, out int zsDictionaryId);

        if (_tk.IsVanilla(path.Canonical, src.Span, path.FileVersion)) {
            return;
        }

        using RentedBuffer<byte> vanilla
            = _tk.GetVanilla(canonical, path.Attributes);

        if (vanilla.IsEmpty) {
            AddChangelogMetadata(path, canonical, ChangelogEntryType.Copy, zsDictionaryId);
            outputFilePath = Path.Combine(path.Root.ToString(), canonical);
            using Stream output = _writer.OpenWrite(outputFilePath);
            output.Write(raw.Span);
            return;
        }

        builder.Build(canonical, path, src.Segment, vanilla.Segment, (path, canon) => {
            AddChangelogMetadata(path, canon, ChangelogEntryType.Changelog, zsDictionaryId);
            string outputFile = Path.Combine(path.Root.ToString(), canon);
            return _writer.OpenWrite(outputFile);
        });
    }

    private void AddChangelogMetadata(in TkPath path, string canonical, ChangelogEntryType type, int zsDictionaryId)
    {
        if (path.Canonical.Length > 4 && path.Canonical[..4] is "Mals") {
            _changelog.MalsFiles.Add(canonical);
            return;
        }

        _changelog.ChangelogFiles.Add(
            new TkChangelogEntry(
                canonical, type, path.Attributes, zsDictionaryId
            )
        );
    }

    internal static ITkChangelogBuilder? GetChangelogBuilder(in TkPath path)
    {
        return path switch {
            { Canonical: "GameData/GameDataList.Product.byml" } => GameDataChangelogBuilder.Instance,
            { Canonical: "RSDB/Tag.Product.rstbl.byml" } => RsdbTagChangelogBuilder.Instance,
            { Canonical: "RSDB/GameSafetySetting.Product.rstbl.byml" } => RsdbRowChangelogBuilder.NameHash,
            { Canonical: "RSDB/RumbleCall.Product.rstbl.byml" or "RSDB/UIScreen.Product.rstbl.byml" } => RsdbRowChangelogBuilder.Name,
            { Canonical: "RSDB/TagDef.Product.rstbl.byml" } => RsdbRowChangelogBuilder.FullTagId,
            {
                Canonical: "RSDB/ActorInfo.Product.rstbl.byml" or
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
                "RSDB/XLinkPropertyTableList.Product.rstbl.byml"
            } => RsdbRowChangelogBuilder.RowId,
            { Extension: ".msbt" } => MsbtChangelogBuilder.Instance,
            { Extension: ".bfarc" or ".bkres" or ".blarc" or ".genvb" or ".pack" or ".sarc" or ".ta" } => SarcChangelogBuilder.Instance,
            { Extension: ".bgyml" } => BymlChangelogBuilder.Instance,
            { Extension: ".byml" } when path.Canonical[..4] is not "RSDB" && path.Canonical[..8] is not "GameData" => BymlChangelogBuilder.Instance,
            _ => null
        };
    }
}