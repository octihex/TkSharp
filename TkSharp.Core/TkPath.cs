using TkSharp.Core.Extensions;

namespace TkSharp.Core;

public readonly ref struct TkPath(ReadOnlySpan<char> canonical, int fileVersion, TkFileAttributes attributes, ReadOnlySpan<char> root, string path)
{
    public readonly ReadOnlySpan<char> Canonical = canonical;

    public readonly int FileVersion = fileVersion;

    public readonly TkFileAttributes Attributes = attributes;

    public readonly ReadOnlySpan<char> Root = root;

    public readonly string Path = path;

    /// <summary>
    /// Parse a path and root into a <see cref="TkPath"/> struct.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="rootFolderPath"></param>
    /// <returns></returns>
    public static TkPath FromPath(string path, string rootFolderPath)
    {
        if (rootFolderPath.Length >= path.Length) {
            throw new ArgumentException(
                "The root of the path must be longer than the path itself.", nameof(rootFolderPath)
            );
        }

        ReadOnlySpan<char> span = path.AsSpan();
        ReadOnlySpan<char> relative = span[rootFolderPath.Length..];

        if (relative.Length < 6) {
            throw new ArgumentException(
                "The path must contain a root folder or 'romfs', 'exefs' or 'cheats'.", nameof(path)
            );
        }

        Span<char> root = stackalloc char[6];
        relative.ToLowerInvariant(root);

        int rootLength = root[..5] switch {
            "romfs" or "exefs" => 5,
            "cheat" when root[^1] is 's' => 6,
            _ => throw new ArgumentException(
                "The path must contain a root folder or 'romfs', 'exefs' or 'cheats'.", nameof(path)
            )
        };

        ReadOnlySpan<char> canonical = span
            .GetCanonical(span[(rootFolderPath.Length + rootLength)..],
                out int fileVersion, out TkFileAttributes attributes
            );

        return new TkPath(
            canonical,
            fileVersion,
            attributes,
            relative[..rootLength],
            path
        );
    }
}