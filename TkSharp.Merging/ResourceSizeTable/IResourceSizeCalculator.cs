namespace TkSharp.Merging.ResourceSizeTable;

public interface IResourceSizeCalculator
{
    static abstract int MinBufferSize { get; }
    
    static abstract uint GetResourceSize(in Span<byte> data);
}