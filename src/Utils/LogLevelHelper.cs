using Microsoft.Extensions.Logging;

namespace EnvAutoUpdater.src.Utils
{
    public static class LogLevelHelper
    {
        private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            { "trace", "Trace" },
            { "debug", "Debug" },
            { "info", "Information" },
            { "information", "Information" },
            { "warn", "Warning" },
            { "warning", "Warning" },
            { "error", "Error" },
            { "critical", "Critical" },
            { "crit", "Critical" },
            { "none", "None" }
        };

        public static LogLevel Parse(string? value, LogLevel fallback = LogLevel.Information)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var normalized = _map.TryGetValue(value, out var mapped) ? mapped : value;
            return Enum.TryParse<LogLevel>(normalized, true, out var result) ? result : fallback;
        }
    }
}
