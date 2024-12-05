using TkSharp.Core;

namespace TkSharp.DevTools.Components;

public class DirectRomProvider(ITkRom rom) : ITkRomProvider
{
    public ITkRom GetRom()
    {
        return rom;
    }
}