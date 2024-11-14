using TkSharp.Core;

namespace TkSharp.Merging;

public delegate Stream OpenWriteChangelog(TkPath path, string canonical);

public interface ITkChangelogBuilder
{
    void Build(string canonical, in TkPath path, ArraySegment<byte> srcBuffer, ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite);
}