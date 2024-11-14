using TkSharp.Core;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Merging;

public delegate Stream OpenWriteChangelog(TkPath path, string canonical);

public interface ITkChangelogBuilder
{
    void Build(in TkPath path, RentedBuffer<byte> src, RentedBuffer<byte> vanilla, OpenWriteChangelog openWrite);
}