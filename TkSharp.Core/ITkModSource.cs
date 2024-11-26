namespace TkSharp.Core;

public interface ITkModSource
{
    string PathToRoot { get; }
    
    IEnumerable<(string FilePath, object Entry)> Files { get; }

    Stream OpenRead(object entry);
}