using System.Runtime.CompilerServices;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core;

public interface ITkRom
{
    int GameVersion { get; }
    
    string NsoBinaryId { get; }
    
    TkZstd Zstd { get; }
    
    // TODO: Type-specific tables should also be loaded
    IDictionary<string, string> AddressTable { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string CanonicalToRelativePath(string canonical, TkFileAttributes attributes)
    {
        string result = AddressTable.TryGetValue(canonical, out string? address)
            ? address
            : canonical;
        
        if (attributes.HasFlag(TkFileAttributes.HasZsExtension)) {
            result += ".zs";
        }
        
        // Until we can decode .mc files this will never be reached
        // 
        // if (attributes.HasFlag(TkFileAttributes.HasMcExtension)) {
        //     relativePath += ".mc";
        // }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool VanillaFileExists(string canonical, TkFileAttributes attributes)
    {
        return VanillaFileExists(
            CanonicalToRelativePath(canonical, attributes)
        );
    }
    
    bool VanillaFileExists(string relativeFilePath);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    RentedBuffer<byte> GetVanilla(string canonical, TkFileAttributes attributes)
    {
        return GetVanilla(
            CanonicalToRelativePath(canonical, attributes)
        );
    }
    
    RentedBuffer<byte> GetVanilla(string relativeFilePath);
    
    bool IsVanilla(ReadOnlySpan<char> canonical, Span<byte> src, int fileVersion);
}