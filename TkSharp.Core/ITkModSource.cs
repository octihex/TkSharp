namespace TkSharp.Core;

public interface ITkModSource
{
    string PathToRoot { get; }
    
    string[] Files { get; }

    Stream OpenRead(string file);
}