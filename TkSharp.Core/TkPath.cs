using TkSharp.Core.Extensions;

namespace TkSharp.Core;

public readonly ref struct TkPath(ReadOnlySpan<char> canonical, int fileVersion, TkFileAttributes attributes, ReadOnlySpan<char> root, ReadOnlySpan<char> extension, string originPath)
{
    public readonly ReadOnlySpan<char> Canonical = canonical;

    public readonly int FileVersion = fileVersion;

    public readonly TkFileAttributes Attributes = attributes;

    public readonly ReadOnlySpan<char> Root = root;

    public readonly ReadOnlySpan<char> Extension = extension;

    public readonly string OriginPath = originPath;

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

        int rootFolderLength = rootFolderPath[^1] switch {
            '/' or '\\' => rootFolderPath.Length,
            _ => rootFolderPath.Length + 1
        };

        ReadOnlySpan<char> span = path.AsSpan();
        ReadOnlySpan<char> relative = span[rootFolderLength..];

        if (relative.Length < 6) {
            throw new ArgumentException(
                "The path must contain a root folder or 'romfs', 'exefs' or 'cheats'.", nameof(path)
            );
        }

        Span<char> root = stackalloc char[6];
        relative[..6].ToLowerInvariant(root);

        int rootLength = root[..5] switch {
            "romfs" or "exefs" => 5,
            "cheat" when root[^1] is 's' => 6,
            _ => throw new ArgumentException(
                "The path must contain a root folder or 'romfs', 'exefs' or 'cheats'.", nameof(path)
            )
        };

        ReadOnlySpan<char> canonical = span[(rootFolderLength + rootLength + 1)..].GetCanonical(
            out int fileVersion, out TkFileAttributes attributes
        );

        return new TkPath(
            canonical,
            fileVersion,
            attributes,
            relative[..rootLength],
            Path.GetExtension(canonical),
            path
        );
    }
}