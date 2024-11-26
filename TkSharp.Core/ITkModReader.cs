using TkSharp.Core.Models;

namespace TkSharp.Core;

public interface ITkModReader
{
    ValueTask<TkMod?> ReadMod(object? input, Stream? stream = null, TkModContext context = default, CancellationToken ct = default);

    bool IsKnownInput(object? input);
}