namespace TkSharp.Merging.ResourceSizeTable;

public interface ITkResourceSizeCalculator
{
    static abstract int MinBufferSize { get; }
    
    static abstract uint GetResourceSize(in Span<byte> data);
}