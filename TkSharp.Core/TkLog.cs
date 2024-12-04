using Microsoft.Extensions.Logging;
using TkSharp.Core.Common;

namespace TkSharp.Core;

public class TkLog : Singleton<TkLog>, ILogger
{
    private readonly List<ILogger> _loggers = [];
    
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    public void Register(ILogger logger)
    {
        _loggers.Add(logger);
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (ILogger logger in _loggers) {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default;
    }
}