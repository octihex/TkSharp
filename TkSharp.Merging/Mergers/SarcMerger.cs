using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.ResourceSizeTable;

namespace TkSharp.Merging.Mergers;

public sealed class SarcMerger : Singleton<SarcMerger>, ITkMerger
{
    public void Merge(RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output, TkResourceSizeCollector tkResourceSizeTable)
    {
        throw new NotImplementedException();
    }
}