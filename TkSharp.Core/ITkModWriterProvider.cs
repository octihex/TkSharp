using TkSharp.Core.Models;

namespace TkSharp.Core;

public interface ITkModWriterProvider
{
    ITkModWriter GetSystemWriter(TkModContext modContext);
}