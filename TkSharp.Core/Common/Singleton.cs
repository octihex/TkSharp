namespace TkSharp.Core.Common;

public abstract class Singleton<T> where T : Singleton<T>, new()
{
    public static readonly T Instance = new();
}