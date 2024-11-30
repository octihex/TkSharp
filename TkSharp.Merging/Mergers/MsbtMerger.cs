using MessageStudio.Formats.BinaryText;
using TkSharp.Core.IO.Buffers;
using TkSharp.Core.Models;

namespace TkSharp.Merging.Mergers;

public sealed class MsbtMerger : Singleton<MsbtMerger>, ITkMerger
{
    public void Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        throw new NotImplementedException();
    }

    public void MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        Msbt baseMsbt = Msbt.FromBinary(@base);
        Msbt changelog = Msbt.FromBinary(input);

        foreach ((string key, MsbtEntry value) in changelog) {
            baseMsbt[key] = value;
        }
        
        baseMsbt.WriteBinary(output);
    }
}