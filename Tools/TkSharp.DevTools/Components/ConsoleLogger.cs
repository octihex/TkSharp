using Kokuban;
using Kokuban.AnsiEscape;
using Microsoft.Extensions.Logging;

namespace TkSharp.DevTools.Components;

public class ConsoleLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        AnsiStyle style = logLevel switch {
            LogLevel.Trace => Chalk.Underline,
            LogLevel.Critical => Chalk.Red,
            LogLevel.Error => Chalk.BrightRed,
            LogLevel.Debug => Chalk.Rgb(245, 155, 66),
            LogLevel.Information => Chalk.BrightBlue,
            LogLevel.Warning => Chalk.BrightYellow,
            _ => Chalk.White,
        };

        Console.WriteLine(
            Chalk.Bold + $"[{DateTime.UtcNow:s}] [{eventId}] "
                       + style + formatter(state, exception)
        );
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        throw new NotImplementedException();
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}