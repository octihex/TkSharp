using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core.IO.Parsers;

public static class EffectInfoParser
{
    public static Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> ParseFileEntry(Span<byte> src, TkZstd zstd)
    {
        using RentedBuffer<byte> decompressed = RentedBuffer<byte>.Allocate(TkZstd.GetDecompressedSize(src));
        zstd.Decompress(src, decompressed.Span);

        BymlMap table = Byml.FromBinary(decompressed.Span)
            .GetMap(); 
        
        Dictionary<string, string> result = table["BinaryDict"]
            .GetMap()
            .Select(kvp => (kvp.Key, Value: kvp.Value.GetString()))
            .Where(kvp => kvp.Value.Length > 11 && kvp.Value[^11..^4] is "Product")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        result["static"] = table["StaticEsetb"]
            .GetArray()[0]
            .GetString();

        return result.GetAlternateLookup<ReadOnlySpan<char>>();
    }
}