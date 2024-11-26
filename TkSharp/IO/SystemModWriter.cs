using TkSharp.Core;

namespace TkSharp.IO;

public sealed class SystemModWriter(TkModManager manager, Ulid id) : ITkModWriter
{
    private readonly string _rootFolder = Path.Combine(manager.ModsFolderPath, id.ToString());
    
    public Stream OpenWrite(string filePath)
    {
        string outputFilePath = Path.Combine(_rootFolder, filePath);
        if (Path.GetDirectoryName(outputFilePath) is string folderPath) {
            Directory.CreateDirectory(folderPath);
        }
        
        return File.Create(outputFilePath);
    }
}