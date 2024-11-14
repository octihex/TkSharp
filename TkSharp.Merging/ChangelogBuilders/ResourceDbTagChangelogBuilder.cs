using TkSharp.Core;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Merging.ChangelogBuilders;

public sealed class ResourceDbTagChangelogBuilder : Singleton<ResourceDbTagChangelogBuilder>, ITkChangelogBuilder
{
    public void Build(in TkPath path, RentedBuffer<byte> src, RentedBuffer<byte> vanilla)
    {
        throw new NotImplementedException();
    }
}