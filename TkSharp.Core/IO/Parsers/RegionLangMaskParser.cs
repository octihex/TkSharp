namespace TkSharp.Core.IO.Parsers;

public static class RegionLangMaskParser
{
    public static int ParseVersion(ReadOnlySpan<byte> src)
    {
        for (int i = src.Length - 1; i > -1; i--) {
            if (src[i] != '\n') {
                continue;
            }

            if (i < 4) {
                goto InvalidInput;
            }
            
            return int.Parse(src[(i - 4)..(i - 1)]);
        }

    InvalidInput:
        throw new ArgumentException(
            "Invalid RegionLangMask contents: the version range could not be found.",
            nameof(src));
    }
}