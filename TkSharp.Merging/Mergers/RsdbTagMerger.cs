using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;

namespace TkSharp.Merging.Mergers;

public sealed class RsdbTagMerger : Singleton<RsdbTagMerger>, ITkMerger
{
    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        throw new NotImplementedException();
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        throw new NotImplementedException();
    }
}