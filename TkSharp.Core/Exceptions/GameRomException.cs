namespace TkSharp.Core.Exceptions;

public sealed class GameRomException : Exception
{
    public GameRomException(string message) : base(message)
    {
    }
    
    public GameRomException(string message, Exception innerException) : base(message, innerException)
    {
    }
}