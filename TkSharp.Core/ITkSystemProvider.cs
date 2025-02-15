using TkSharp.Core.Models;

namespace TkSharp.Core;

public interface ITkSystemProvider
{
    ITkModWriter GetSystemWriter(TkModContext context);
    
    ITkSystemSource GetSystemSource(string relativeFolderPath);
}