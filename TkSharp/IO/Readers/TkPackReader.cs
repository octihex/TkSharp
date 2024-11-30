using Revrs.Extensions;
using SharpCompress.Common.Zip;
using SharpCompress.Readers.Zip;
using TkSharp.Core;
using TkSharp.Core.Models;
using TkSharp.IO.Serialization;
using static TkSharp.IO.Serialization.TkPackWriter;

namespace TkSharp.IO.Readers;

public sealed class TkPackReader(ITkSystemProvider systemProvider) : ITkModReader
{
    private readonly ITkSystemProvider _systemProvider = systemProvider;

    public async ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext context = default, CancellationToken ct = default)
    {
        if (input is not string) {
            throw new ArgumentException(
                "Input value must be a string.", nameof(input)
            );
        }

        if (stream is null) {
            throw new ArgumentException(
                "Input stream must not be null.", nameof(stream)
            );
        }

        if (stream.Read<uint>() != MAGIC) {
            throw new InvalidDataException(
                "Invalid TotK mod pack magic.");
        }

        if (stream.Read<uint>() != VERSION) {
            throw new InvalidDataException(
                "Unexpected TotK mod pack version. Expected 1.0.0");
        }

        TkMod result = TkBinaryReader.ReadTkMod(stream, _systemProvider);
        context.Id = result.Id;

        ZipReader reader = ZipReader.Open(stream);
        
        ITkModWriter writer = _systemProvider.GetSystemWriter(context);
        while (reader.MoveToNextEntry()) {
            ZipEntry entry = reader.Entry;
            await using Stream archiveStream = reader.OpenEntryStream();
            await using Stream output = writer.OpenWrite(entry.Key!);
            await archiveStream.CopyToAsync(output, ct);
        }

        return result;
    }

    public bool IsKnownInput(object? input)
    {
        return input is string path &&
               Path.GetExtension(path.AsSpan()) is ".tkcl";
    }
}