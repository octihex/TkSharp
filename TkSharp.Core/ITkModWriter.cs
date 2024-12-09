namespace TkSharp.Core;

public interface ITkModWriter
{
    Stream OpenWrite(string filePath);
    
    void SetRelativeFolder(string rootFolder);
}