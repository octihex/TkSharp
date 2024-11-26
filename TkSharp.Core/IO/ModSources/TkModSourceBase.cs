namespace TkSharp.Core.IO.ModSources;

public abstract class TkModSourceBase<T>(string romfsPath) : ITkModSource where T : notnull
{
    public virtual string PathToRoot { get; } = romfsPath;

    protected abstract IEnumerable<T> Files { get; }

    IEnumerable<(string FilePath, object Entry)> ITkModSource.Files
        => Files.Select(x => (GetFileName(x), (object)x));

    public Stream OpenRead(object entry)
    {
        return OpenRead((T)entry);
    }
    
    protected abstract Stream OpenRead(T entry);
    
    protected abstract string GetFileName(T input);
}