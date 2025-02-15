using TkSharp.Core.Models;

namespace TkSharp.Core;

public interface ITkModReader
{
    ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext? context = null, CancellationToken ct = default)
    {
        context ??= new TkModContext(Ulid.Empty, input, stream);
        return ReadMod(context, ct);
    }
    
    ValueTask<TkMod?> ReadMod(TkModContext context, CancellationToken ct = default);

    bool IsKnownInput(object? input);
}