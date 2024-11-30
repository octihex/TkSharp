using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.ResourceSizeTable;

namespace TkSharp.Merging.Mergers;

public sealed class RsdbRowMerger(string rowKey) : ITkMerger
{
    public static readonly RsdbRowMerger RowId = new RsdbRowMerger("__RowId");
    public static readonly RsdbRowMerger Name = new RsdbRowMerger("Name");
    public static readonly RsdbRowMerger FullTagId = new RsdbRowMerger("FullTagId");
    
    public void Merge(RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output, TkResourceSizeCollector tkResourceSizeTable)
    {
        throw new NotImplementedException();
    }
}