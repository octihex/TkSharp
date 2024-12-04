using System.Diagnostics.Contracts;
using CommunityToolkit.HighPerformance.Buffers;
using RstbLibrary;
using RstbLibrary.Helpers;
using TkSharp.Core;
using TkSharp.Core.IO.Buffers;
using TkSharp.Merging.ResourceSizeTable.Calculators;

namespace TkSharp.Merging.ResourceSizeTable;

// ReSharper disable StringLiteralTypo
public sealed class TkResourceSizeCollector(ITkModWriter writer, ITkRom rom)
{
    private readonly ITkModWriter _writer = writer;
    private readonly ITkRom _rom = rom;
    private readonly Dictionary<string, uint> _updates = [];
    private readonly Dictionary<string, uint> _additions = [];

    public void Write()
    {
        string relativePath = $"System/Resource/ResourceSizeTable.Product.{_rom.GameVersion}.rsizetable.zs";
        using RentedBuffer<byte> vanillaRstb = _rom.GetVanilla(relativePath);
        Rstb result = Rstb.FromBinary(vanillaRstb.Span);

        foreach ((string name, uint value) in _updates) {
            if (result.OverflowTable.ContainsKey(name)) {
                result.OverflowTable[name] = value;
                continue;
            }

            uint hash = Crc32.Compute(name);
            result.HashTable[hash] = value;
        }

        foreach ((string name, uint value) in _additions) {
            uint hash = Crc32.Compute(name);
            if (!result.HashTable.TryAdd(hash, value)) {
                result.OverflowTable[name] = value;
            }
        }

        using MemoryStream ms = new();
        result.WriteBinary(ms);

        if (!ms.TryGetBuffer(out ArraySegment<byte> buffer)) {
            buffer = ms.ToArray();
        }
        
        using SpanOwner<byte> compressed = SpanOwner<byte>.Allocate(buffer.Count);
        Span<byte> compressedData = compressed.Span;
        int compressedSize = _rom.Zstd.Compress(buffer, compressedData);

        using Stream output = _writer.OpenWrite(Path.Combine("romfs", relativePath));
        output.Write(compressedData[..compressedSize]);
    }

    public void Collect(int fileSize, string canonical, bool isFileVanillaEntry, in Span<byte> data)
    {
        ReadOnlySpan<char> extension = Path.GetExtension(canonical.AsSpan());
        if (canonical is "Pack/ZsDic.pack" || extension is ".rsizetable" or ".bwav" or ".webm") {
            return;
        }
        
        Dictionary<string, uint> resources = isFileVanillaEntry switch {
            true => _updates,
            false => _additions,
        };

        lock (resources) {
            resources[canonical] = GetResourceSize(
                (uint)fileSize,
                canonical,
                Path.GetExtension(canonical.AsSpan()),
                data);
        }
    }

    [Pure]
    public static bool RequiresDataForCalculation(ReadOnlySpan<char> extension)
    {
        return extension is ".ainb" or ".asb" or ".bstar" or ".mc";
    }

    [Pure]
    private static uint GetResourceSize(uint size, ReadOnlySpan<char> canonical, ReadOnlySpan<char> extension, Span<byte> data)
    {
        return canonical switch {
            "Event/EventFlow/Dm_ED_0004.bfevfl" => size + 0x1E0,
            "Effect/static.Nin_NX_NVN.esetb.byml" => size + 0x1000,
            "Effect/static.Product.110.Nin_NX_NVN.esetb.byml" => size + 0x1000,
            "Lib/agl/agl_resource.Nin_NX_NVN.release.sarc" => size + 0x1000,
            "Lib/gsys/gsys_resource.Nin_NX_NVN.release.sarc" => size + 0x1000,
            "Lib/Terrain/tera_resource.Nin_NX_NVN.release.sarc" => size + 0x1000,
            "Shader/ApplicationPackage.Nin_NX_NVN.release.sarc" => size + 0x1000,
            _ => extension switch {
                ".ainb" => size + AinbResourceSizeCalculator.GetResourceSize(data),
                ".asb" => size + AsbResourceSizeCalculator.GetResourceSize(data),
                ".bgyml" => (size + 1000) * 8,
                ".baatarc" => size + 0x100,
                ".baev" => size + 0x120,
                ".bagst" => size + 0x100,
                ".bars" => size + 0x240,
                ".bcul" => size + 0x100,
                ".beco" => size + 0x100,
                ".belnk" => size + 0x100,
                ".bfarc" => size + 0x100,
                ".bfevfl" => size + 0x120,
                ".bfsha" => size + 0x100,
                ".bhtmp" => size + 0x100,
                ".blal" => size + 0x100,
                ".blarc" => size + 0x100,
                ".blwp" => size + 0x100,
                ".bnsh" => size + 0x100,
                ".bntx" => size + 0x100,
                ".bphcl" => size + 0x100,
                ".bphhb" => size + 0x100,
                ".bphnm" => size + 0x120,
                ".bphsh" => size + 0x170,
                ".bslnk" => size + 0x100,
                ".bstar" => size + BstarResourceSizeCalculator.GetResourceSize(data),
                ".byml" => size + 0x100,
                ".cai" => size + 0x100,
                ".casset.byml" => size + 0x1C0,
                ".chunk" => size + 0x100,
                ".crbin" => size + 0x100,
                ".cutinfo" => size + 0x100,
                ".dpi" => size + 0x100,
                ".genvb" => size + 0x180,
                ".jpg" => size + 0x100,
                ".mc" => (size + ModelCodecResourceCalculator.GetResourceSize(data) + 1500) * 4,
                ".pack" => size + 0x180,
                ".png" => size + 0x100,
                ".quad" => size + 0x100,
                ".sarc" => size + 0x180,
                ".tscb" => size + 0x100,
                ".txtg" => size + 0x100,
                ".txt" => size + 0x100,
                ".vsts" => size + 0x100,
                ".wbr" => size + 0x100,
                ".zs" => size + 0x100,
                _ => (size + 1500) * 4
            }
        };
    }
}