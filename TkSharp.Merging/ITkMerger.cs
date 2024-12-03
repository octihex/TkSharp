using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;

namespace TkSharp.Merging;

public interface ITkMerger
{
    void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output);
    
    void Merge(TkChangelogEntry entry, IEnumerable<ArraySegment<byte>> inputs, ArraySegment<byte> vanillaData, Stream output);
    
    void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output);
}