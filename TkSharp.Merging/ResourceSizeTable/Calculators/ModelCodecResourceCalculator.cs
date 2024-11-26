using System.Runtime.CompilerServices;
using Revrs.Extensions;

namespace TkSharp.Merging.ResourceSizeTable.Calculators;

public sealed class ModelCodecResourceCalculator : IResourceSizeCalculator
{
    public static int MinBufferSize => 0xC;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static uint GetResourceSize(in Span<byte> data)
    {
        int flags = data[0x8..0xC].Read<int>();
        int size = (flags >> 5) << (flags & 0xF);
        return (uint)((size * 1.15 + 0x1000) * 4);
    }
}