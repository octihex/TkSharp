using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.ResourceSizeTable;

namespace TkSharp.Merging.Mergers;

public sealed class BymlMerger : Singleton<BymlMerger>, ITkMerger
{
    public void Merge(RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output, TkResourceSizeCollector tkResourceSizeTable)
    {
        throw new NotImplementedException();
    }
}