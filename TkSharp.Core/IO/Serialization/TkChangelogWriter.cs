using CommunityToolkit.HighPerformance;
using TkSharp.Core.Extensions;
using TkSharp.Core.Models;

namespace TkSharp.Core.IO.Serialization;

public static class TkChangelogWriter
{
    internal const uint MAGIC = 0x4C434B54; 
    
    public static void Write(in Stream output, TkChangelog changelog)
    {
        output.Write(MAGIC);
        output.Write(changelog.BuilderVersion);
        output.Write(changelog.GameVersion);
        WriteChangelogFiles(output, changelog.ChangelogFiles);
        WriteFileList(output, changelog.MalsFiles);
        WritePatchFiles(output, changelog.PatchFiles);
        WriteFileList(output, changelog.SubSdkFiles);
        WriteFileList(output, changelog.ExeFiles);
        WriteFileList(output, changelog.CheatFiles);
    }

    private static void WriteChangelogFiles(in Stream output, IList<TkChangelogEntry> changelogs)
    {
        output.Write(changelogs.Count);

        foreach (TkChangelogEntry changelog in changelogs) {
            output.WriteString(changelog.Canonical);
            output.Write(changelog.Type);
            output.Write(changelog.Attributes);
            output.Write(changelog.ZsDictionaryId);
        }
    }

    private static void WritePatchFiles(in Stream output, IList<TkPatch> patches)
    {
        output.Write(patches.Count);

        foreach (TkPatch patch in patches) {
            output.WriteString(patch.NsoBinaryId);

            output.Write(patch.Entries.Count);
            foreach ((uint key, uint value) in patch.Entries) {
                output.Write(key);
                output.Write(value);
            }
        }
    }

    private static void WriteFileList(in Stream output, IList<string> files)
    {
        output.Write(files.Count);

        foreach (string file in files) {
            output.WriteString(file);
        }
    }
}