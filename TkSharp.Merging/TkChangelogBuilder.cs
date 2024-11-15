using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
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

    public async ValueTask<TkChangelog> BuildAsync()
    {
        await Task.WhenAll(_source.Files.Select(
                file => Task.Run(() => BuildTarget(file))
            ))
            .ConfigureAwait(false);

        return _changelog;
    }

    public TkChangelog Build()
    {
        foreach (string file in _source.Files) {
            BuildTarget(file);
        }

        return _changelog;
    }

    private void BuildTarget(string file)
    {
        TkPath path = TkPath.FromPath(file, _source.PathToRoot);
        string canonical = path.Canonical.ToString();

        using Stream content = _source.OpenRead(file);

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
            case { Extension: ".ini" }:
                return;
        }

        goto Build;

    CopyWithMetadata:
        AddChangelogMetadata(path, canonical, ChangelogEntryType.Copy, zsDictionaryId: -1);

    Copy:
        string outputFilePath = Path.Combine(path.Root.ToString(), canonical);
        // ReSharper disable once ConvertToUsingDeclaration
        using (Stream output = _writer.OpenWrite(outputFilePath)) {
            content.Seek(0, SeekOrigin.Begin);
            content.CopyTo(output);
        }

        return;

    Build:
        if (GetChangelogBuilder(path) is not ITkChangelogBuilder builder) {
            goto CopyWithMetadata;
        }

        RentedBuffer<byte> src = _tk.Zstd.Decompress(content, out int zsDictionaryId);

        if (_tk.IsVanilla(path.Canonical, src.Span, path.FileVersion)) {
            return;
        }

        RentedBuffer<byte> vanilla
            = _tk.GetVanilla(canonical, path.Attributes);

        if (vanilla.IsEmpty) {
            goto CopyWithMetadata;
        }

        try {
            builder.Build(canonical, path, src.Segment, vanilla.Segment, (path, canon) => {
                AddChangelogMetadata(path, canon, ChangelogEntryType.Changelog, zsDictionaryId);
                string outputFile = Path.Combine(path.Root.ToString(), canon);
                return _writer.OpenWrite(outputFile);
            });
        }
        finally {
            src.Dispose();
            vanilla.Dispose();
        }
    }

    private void AddChangelogMetadata(in TkPath path, string canonical, ChangelogEntryType type, int zsDictionaryId)
    {
        if (path.Canonical.Length > 4 && path.Canonical[..4] is "Mals") {
            _changelog.MalsFiles.Add(canonical);
            return;
        }

        _changelog.ChangelogFiles.Add(
            (canonical, new TkChangelogEntry(
                type, path.Attributes, zsDictionaryId
            ))
        );
    }

    internal static ITkChangelogBuilder? GetChangelogBuilder(in TkPath path)
    {
        return path switch {
            { Canonical: "GameData/GameDataList.Product.byml" } => GameDataChangelogBuilder.Instance,
            { Canonical: "RSDB/Tag.Product.rstbl.byml" } => ResourceDbTagChangelogBuilder.Instance,
            { } when path.Canonical[..4] is "RSDB" => ResourceDbRowChangelogBuilder.Instance,
            { Extension: ".msbt" } => MsbtChangelogBuilder.Instance,
            { Extension: ".bfarc" or ".bkres" or ".blarc" or ".genvb" or ".pack" or ".sarc" or ".ta" } => SarcChangelogBuilder.Instance,
            { Extension: ".bgyml" } => BymlChangelogBuilder.Instance,
            { Extension: ".byml" } when path.Canonical[..4] is not "RSDB" && path.Canonical[..8] is not "GameData" => BymlChangelogBuilder.Instance,
            _ => null
        };
    }
}