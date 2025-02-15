using TkSharp.Core.Models;

namespace TkSharp.Core;

public interface ITkModReader
{
    ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext? context = null, CancellationToken ct = default)
    {
        context ??= new TkModContext(Ulid.Empty);
        context.Input = input;
        context.Stream = stream;
        return ReadMod(context, ct);
    }
    
    ValueTask<TkMod?> ReadMod(TkModContext context, CancellationToken ct = default);

    bool IsKnownInput(object? input);
}