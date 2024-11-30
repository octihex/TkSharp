using System.Runtime.CompilerServices;
using Revrs.Extensions;

namespace TkSharp.Merging.ResourceSizeTable.Calculators;

public sealed class AsbResourceSizeCalculator : ITkResourceSizeCalculator
{
    public static int MinBufferSize => -1;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static uint GetResourceSize(in Span<byte> data)
    {
        uint nodeCount = data[0x10..0x14].Read<uint>();
        int exbOffset = data[0x60..0x64].Read<int>();
        uint size = 552 + 40 * nodeCount;

        if (exbOffset != 0) {
            int exbCountOffset = data[(exbOffset + 0x20)..].Read<int>();
            uint exbSignatureCount = data[(exbOffset + exbCountOffset)..].Read<uint>();
            size += 16 + (exbSignatureCount + 1) / 2 * 8;
        }

        return size;
    }
}