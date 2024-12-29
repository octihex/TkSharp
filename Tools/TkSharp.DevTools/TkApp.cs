using TkSharp.Core;
using TkSharp.DevTools.Components;
using TkSharp.DevTools.Helpers;
using TkSharp.Extensions.GameBanana.Readers;

namespace TkSharp.DevTools;

public static class TkApp
{
    static TkApp()
    {
        TkLog.Instance.Register(new ConsoleLogger());
        ReaderProvider.Register(new GameBananaModReader(ReaderProvider));
    }

    public static ITkRomProvider TkRomProvider => RomHelper.Instance;

    public static readonly TkModManager ModManager = TkModManager.CreatePortable();

    public static readonly TkModReaderProvider ReaderProvider = new(ModManager, TkRomProvider);
}