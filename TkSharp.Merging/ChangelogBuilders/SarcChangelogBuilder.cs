using SarcLibrary;
using TkSharp.Core;

namespace TkSharp.Merging.ChangelogBuilders;

public sealed class SarcChangelogBuilder : Singleton<SarcChangelogBuilder>, ITkChangelogBuilder
{
    private static readonly byte[] _deletedFileMark = [0x54, 0x4B, 0x53, 0x43, 0x52, 0x4D, 0x56, 0x44];

    public void Build(string canonical, in TkPath path, ArraySegment<byte> srcBuffer, ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite)
    {
        Sarc vanilla = Sarc.FromBinary(vanillaBuffer);

        Sarc changelog = [];
        Sarc sarc = Sarc.FromBinary(srcBuffer);

        foreach ((string name, ArraySegment<byte> data) in sarc) {
            if (!vanilla.TryGetValue(name, out ArraySegment<byte> vanillaData)) {
                // Custom file, use entire content
                goto MoveContent;
            }

            if (data.AsSpan().SequenceEqual(vanillaData)) {
                // Vanilla file, ignore
                continue;
            }

            var nested = new TkPath(
                name,
                fileVersion: path.FileVersion,
                TkFileAttributes.None,
                root: "romfs",
                name
            );

            if (TkChangelogBuilder.GetChangelogBuilder(nested) is not ITkChangelogBuilder builder) {
                goto MoveContent;
            }

            builder.Build(name, nested, data, vanillaData,
                (_, canon) => changelog.OpenWrite(canon)
            );

            continue;

        MoveContent:
            changelog[name] = data;
        }

        foreach ((string key, _) in vanilla) {
            if (!sarc.ContainsKey(key)) {
                changelog[key] = _deletedFileMark;
            }
        }

        if (changelog.Count == 0) {
            return;
        }

        using Stream output = openWrite(path, canonical);
        changelog.Write(output, changelog.Endianness);
    }
}