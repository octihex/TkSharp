using System.Runtime.CompilerServices;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core;

public interface ITkRom
{
    int GameVersion { get; }
    
    TkZstd Zstd { get; }
    
    IDictionary<string, string> AddressTable { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    RentedBuffer<byte> GetVanillaFromCanonical(string canonical)
    {
        return GetVanilla(
            AddressTable.TryGetValue(canonical, out string? address) ? address : canonical
        );
    }
    
    RentedBuffer<byte> GetVanilla(string relativeFilePath);
    
    bool IsVanilla(ReadOnlySpan<char> canonical, Span<byte> src, int fileVersion);
}