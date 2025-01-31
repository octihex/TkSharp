using System.Runtime.CompilerServices;
using TkSharp.Core.IO.Buffers;

namespace TkSharp.Core;

public interface ITkRom : IDisposable
{
    private const string EVENT_FLOW_FOLDER = "Event/EventFlow";
    private const string EFFECT_FOLDER = "Effect";

    int GameVersion { get; }

    string NsoBinaryId { get; }

    TkZstd Zstd { get; }

    IDictionary<string, string> AddressTable { get; }

    Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> EventFlowVersions { get; }

    Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> EffectVersions { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string CanonicalToRelativePath(string canonical, TkFileAttributes attributes)
    {
        string result = AddressTable.TryGetValue(canonical, out string? address)
            ? address
            : canonical;

        ReadOnlySpan<char> canon = result.AsSpan();
        if (canon.Length > 26 && canon[..15] is EVENT_FLOW_FOLDER && canon[16..^11] is var eventFlowName
            && EventFlowVersions.TryGetValue(eventFlowName, out string? version)) {
            result = $"{EVENT_FLOW_FOLDER}/{eventFlowName}.{version}{Path.GetExtension(canon)}";
        }

        if (attributes.HasFlag(TkFileAttributes.IsProductFile) && canon.Length > 37 && canon[..6] is EFFECT_FOLDER
            && canon[7..^30] is var effectName && EffectVersions.TryGetValue(effectName, out string? effectFileName)) {
            result = $"{EFFECT_FOLDER}/{effectFileName}.Product.Nin_NX_NVN.esetb.byml";
        }

        if (attributes.HasFlag(TkFileAttributes.HasZsExtension)) {
            result += ".zs";
        }

        if (attributes.HasFlag(TkFileAttributes.HasMcExtension)) {
            result += ".mc";
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    RentedBuffer<byte> GetVanilla(string canonical, TkFileAttributes attributes)
    {
        return GetVanilla(
            CanonicalToRelativePath(canonical, attributes)
        );
    }

    RentedBuffer<byte> GetVanilla(string relativeFilePath);

    bool IsVanilla(ReadOnlySpan<char> canonical, Span<byte> src, int fileVersion);
}