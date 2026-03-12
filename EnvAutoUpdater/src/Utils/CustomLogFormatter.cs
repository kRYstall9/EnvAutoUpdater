using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace EnvAutoUpdater.src.Utils
{
    public class CustomLogFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options) : ConsoleFormatter(nameof(CustomLogFormatter))
    {
        private static readonly TimeZoneInfo _tz = LoadTimeZone();

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            var (level, color) = logEntry.LogLevel switch
            {
                LogLevel.Trace => ("TRACE", ConsoleColor.Gray),
                LogLevel.Debug => ("DEBUG", ConsoleColor.Blue),
                LogLevel.Information => ("INFO", ConsoleColor.Green),
                LogLevel.Warning => ("WARN", ConsoleColor.Yellow),
                LogLevel.Error => ("ERROR", ConsoleColor.Red),
                LogLevel.Critical => ("CRITICAL", ConsoleColor.DarkRed),
                _ => ("UNKNOWN", ConsoleColor.White)
            };

            var timestamp = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _tz).ToString("yyyy-MM-dd HH:mm:ss");
            var category = logEntry.Category?.Split('.').Last();
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

            textWriter.WriteLine($"{timestamp} {GetForegroundColorEscapeCode(color)}{level}\x1B[0m: [{category}] {message}");
        }

        private string GetForegroundColorEscapeCode(ConsoleColor color) =>
            color switch
            {
                ConsoleColor.Black => "\x1B[30m",
                ConsoleColor.DarkRed => "\x1B[31m",
                ConsoleColor.DarkGreen => "\x1B[32m",
                ConsoleColor.DarkYellow => "\x1B[33m",
                ConsoleColor.DarkBlue => "\x1B[34m",
                ConsoleColor.DarkMagenta => "\x1B[35m",
                ConsoleColor.DarkCyan => "\x1B[36m",
                ConsoleColor.Gray => "\x1B[37m",
                ConsoleColor.Red => "\x1B[1m\x1B[31m",
                ConsoleColor.Green => "\x1B[1m\x1B[32m",
                ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
                ConsoleColor.Blue => "\x1B[1m\x1B[34m",
                ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
                ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
                ConsoleColor.White => "\x1B[1m\x1B[37m",
                _ => "\x1B[39m"
            };

        private static TimeZoneInfo LoadTimeZone()
        {
            try
            {
                var tzId = Environment.GetEnvironmentVariable("TZ_INFO") ?? "UTC";
                tzId = "Europe/Rome";
                return TimeZoneInfo.FindSystemTimeZoneById(tzId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
