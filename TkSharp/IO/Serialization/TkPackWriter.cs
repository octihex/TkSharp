using CommunityToolkit.HighPerformance;
using TkSharp.Core.Models;

namespace TkSharp.IO.Serialization;

public static class TkPackWriter
{
    internal const uint MAGIC = 0x504D4B54;
    internal const uint VERSION = 0x10;

    public static void Write(in Stream output, TkMod mod, Stream data)
    {
        output.Write(MAGIC);
        output.Write(VERSION);
        
        TkBinaryWriter.WriteTkMod(output, mod);
        data.Seek(0, SeekOrigin.Begin);
        data.CopyTo(output);
    }
}