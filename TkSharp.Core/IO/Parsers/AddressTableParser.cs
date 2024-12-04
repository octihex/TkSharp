using BymlLibrary;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core.IO.Parsers;

public static class AddressTableParser
{
    public static Dictionary<string, string> ParseAddressTable(in ReadOnlySpan<byte> src, TkZstd zstd)
    {
        using RentedBuffer<byte> decompressed = RentedBuffer<byte>.Allocate(TkZstd.GetDecompressedSize(src));
        zstd.Decompress(src, decompressed.Span);

        return Byml.FromBinary(decompressed.Span)
            .GetMap()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetString());
    }

    public static void Append(in Dictionary<string, string> addressTable, in ReadOnlySpan<byte> src, TkZstd zstd)
    {
        using RentedBuffer<byte> decompressed = RentedBuffer<byte>.Allocate(TkZstd.GetDecompressedSize(src));
        zstd.Decompress(src, decompressed.Span);

        foreach ((string key, Byml value) in Byml.FromBinary(decompressed.Span).GetMap()) {
            addressTable[key] = value.GetString();
        }
    }
}