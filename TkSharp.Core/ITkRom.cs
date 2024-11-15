using System.Runtime.CompilerServices;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core;

public interface ITkRom
{
    int GameVersion { get; }
    
    TkZstd Zstd { get; }
    
    IDictionary<string, string> AddressTable { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    RentedBuffer<byte> GetVanillaFromCanonical(string canonical, TkFileAttributes attributes)
    {
        string relativePath = AddressTable.TryGetValue(canonical, out string? address) ? address : canonical;
        
        if (attributes.HasFlag(TkFileAttributes.HasZsExtension)) {
            relativePath += ".zs";
        }
        
        // Until we can decode .mc files this will never be reached
        // 
        // if (attributes.HasFlag(TkFileAttributes.HasMcExtension)) {
        //     relativePath += ".mc";
        // }
        
        return GetVanilla(relativePath);
    }
    
    RentedBuffer<byte> GetVanilla(string relativeFilePath);
    
    bool IsVanilla(ReadOnlySpan<char> canonical, Span<byte> src, int fileVersion);
}