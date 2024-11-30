using TkSharp.Core.Models;

namespace TkSharp.Core;

public interface ITkSystemProvider
{
    ITkModWriter GetSystemWriter(TkModContext modContext);
    
    ITkSystemSource GetSystemSource(string relativeFolderPath);
}