using System.Runtime.CompilerServices;

namespace TkSharp.Core.Extensions;

public static class TkPathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string fileRelativeToRomfs)
    {
        return GetCanonical(fileRelativeToRomfs.AsSpan(), [], out _, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> fileRelativeToRomfs)
    {
        return GetCanonical(fileRelativeToRomfs, [], out _, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string fileRelativeToRomfs, out int fileVersion, out TkFileAttributes attributes)
    {
        return GetCanonical(fileRelativeToRomfs, [], out fileVersion, out attributes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> fileRelativeToRomfs, out int fileVersion, out TkFileAttributes attributes)
    {
        return GetCanonical(fileRelativeToRomfs, [], out fileVersion, out attributes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string file, ReadOnlySpan<char> romfs)
    {
        return GetCanonical(file.AsSpan(), romfs, out _, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this string file, ReadOnlySpan<char> romfs, out int fileVersion, out TkFileAttributes attributes)
    {
        return GetCanonical(file.AsSpan(), romfs, out fileVersion, out attributes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> file, ReadOnlySpan<char> romfs)
    {
        return GetCanonical(file, romfs, out _, out _);
    }

    public static unsafe ReadOnlySpan<char> GetCanonical(this ReadOnlySpan<char> file, ReadOnlySpan<char> romfs, out int fileVersion, out TkFileAttributes attributes)
    {
        if (file.Length < romfs.Length) {
            throw new ArgumentException(
                $"The provided {nameof(romfs)} path is longer than the input {nameof(file)}.", nameof(romfs)
            );
        }

        fileVersion = -1;
        attributes = TkFileAttributes.None;

        int size = file.Length - romfs.Length - file[^3..] switch {
            ".zs" => (int)(attributes |= TkFileAttributes.HasZsExtension) + 2,
            ".mc" => (int)(attributes |= TkFileAttributes.HasMcExtension) + 1,
            _ => 0
        };

        // Make a copy to avoid
        // mutating the input string
        string result = file[romfs.Length..(romfs.Length + size)].ToString();

        Span<char> canonical;

        fixed (char* ptr = result) {
            canonical = new Span<char>(ptr, size);
        }

        var state = State.Default;
        for (int i = 0; i < size; i++) {
            ref char @char = ref canonical[i];

            if (@char is '.' && size - i > 8 && canonical[i..(i + 8)] is ".Product") {
                attributes |= TkFileAttributes.IsProductFile;
                size -= 4;
                i += 8;
                fileVersion = int.Parse(canonical[(i + 1)..(i + 4)]);
                state = State.SkipProductPart;
            }

            @char = state switch {
                0 => @char,
                _ => @char = canonical[i + 4]
            };

            @char = @char switch {
                '\\' => '/',
                _ => @char
            };
        }
        
        return canonical[0] switch {
            '/' => canonical[1..size],
            _ => canonical[..size]
        };
    }
}

file enum State
{
    Default = 0,
    SkipProductPart = 1
}