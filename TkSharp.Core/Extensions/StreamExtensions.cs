namespace TkSharp.Core.Extensions;

public static class StreamExtensions
{
    public static ArraySegment<byte> GetSpan(this MemoryStream ms)
    {
        if (!ms.TryGetBuffer(out ArraySegment<byte> buffer)) {
            buffer = ms.ToArray();
        }

        return buffer;
    }
}