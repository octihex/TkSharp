using TkSharp.Core;
using TkSharp.Core.Buffers;

namespace TkSharp.Abstractions.IO;

public interface ITkRom
{
    TkZstd Zstd { get; }
    
    ITkAddressTable AddressTable { get; }
    
    RentedBuffer<byte> GetVanilla(string relativeFilePath);
}