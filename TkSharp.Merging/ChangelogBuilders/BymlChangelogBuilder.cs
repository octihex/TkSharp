using TkSharp.Core;

namespace TkSharp.Merging.ChangelogBuilders;

public sealed class BymlChangelogBuilder : Singleton<BymlChangelogBuilder>, ITkChangelogBuilder
{
    public void Build(string canonical, in TkPath path, ArraySegment<byte> srcBuffer, ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite)
    {
        throw new NotImplementedException();
    }
}