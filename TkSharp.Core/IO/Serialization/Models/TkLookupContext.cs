using TkSharp.Core.Models;

namespace TkSharp.Core.IO.Serialization.Models;

public class TkLookupContext()
{
    public Dictionary<TkMod, int> Mods { get; } = [];

    public Dictionary<TkModOptionGroup, int> Groups { get; } = [];

    public Dictionary<TkModOption, int> Options { get; } = [];
}