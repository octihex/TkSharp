using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.ResourceSizeTable;

namespace TkSharp.Merging.Mergers;

public sealed class MsbtMerger : Singleton<MsbtMerger>, ITkMerger
{
    public void Merge(RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output, TkResourceSizeCollector tkResourceSizeTable)
    {
        throw new NotImplementedException();
    }
}