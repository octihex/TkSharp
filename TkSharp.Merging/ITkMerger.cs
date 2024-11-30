using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.ResourceSizeTable;

namespace TkSharp.Merging;

public interface ITkMerger
{
    void Merge(RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output,
        TkResourceSizeCollector tkResourceSizeTable);
}