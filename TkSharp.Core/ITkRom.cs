using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core;

public interface ITkRom
{
    TkZstd Zstd { get; }
    
    IDictionary<string, string> AddressTable { get; }
    
    RentedBuffer<byte> GetVanilla(string relativeFilePath);
}