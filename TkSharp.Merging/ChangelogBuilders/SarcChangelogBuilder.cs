using TkSharp.Core;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Merging.ChangelogBuilders;

public sealed class SarcChangelogBuilder : Singleton<SarcChangelogBuilder>, ITkChangelogBuilder
{
    public void Build(string canonical, in TkPath path, ArraySegment<byte> srcBuffer, ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite)
    {
        throw new NotImplementedException();
    }
}