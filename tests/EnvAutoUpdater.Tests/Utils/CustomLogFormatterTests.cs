using EnvAutoUpdater.src.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EnvAutoUpdater.Tests.Utils
{
    public class CustomLogFormatterTests
    {
        private readonly CustomLogFormatter _formatter;

        public CustomLogFormatterTests()
        {
            var optionsMock = new Mock<IOptionsMonitor<SimpleConsoleFormatterOptions>>();
            optionsMock.Setup(o => o.CurrentValue).Returns(new SimpleConsoleFormatterOptions());
            _formatter = new CustomLogFormatter(optionsMock.Object);
        }

        private string FormatLogEntry(LogLevel logLevel, string message)
        {
            using var writer = new StringWriter();
            var logEntry = new LogEntry<string>(
                logLevel,
                "TestCategory",
                new EventId(0),
                message,
                null,
                (state, ex) => state);

            _formatter.Write(logEntry, null, writer);
            return writer.ToString();
        }

        [Fact]
        public void Write_InformationLevel_ContainsINFO()
        {
            var result = FormatLogEntry(LogLevel.Information, "Test message");

            Assert.Contains("INFO", result);
            Assert.Contains("Test message", result);
        }

        [Fact]
        public void Write_ErrorLevel_ContainsERROR()
        {
            var result = FormatLogEntry(LogLevel.Error, "Error occurred");

            Assert.Contains("ERROR", result);
            Assert.Contains("Error occurred", result);
        }

        [Fact]
        public void Write_DebugLevel_ContainsDEBUG()
        {
            var result = FormatLogEntry(LogLevel.Debug, "Debug info");

            Assert.Contains("DEBUG", result);
        }

        [Fact]
        public void Write_WarningLevel_ContainsWARN()
        {
            var result = FormatLogEntry(LogLevel.Warning, "Warning message");

            Assert.Contains("WARN", result);
        }

        [Fact]
        public void Write_CriticalLevel_ContainsCRITICAL()
        {
            var result = FormatLogEntry(LogLevel.Critical, "Critical error");

            Assert.Contains("CRITICAL", result);
        }

        [Fact]
        public void Write_TraceLevel_ContainsTRACE()
        {
            var result = FormatLogEntry(LogLevel.Trace, "Trace message");

            Assert.Contains("TRACE", result);
        }

        [Fact]
        public void Write_ContainsTimestamp()
        {
            var result = FormatLogEntry(LogLevel.Information, "Test");

            // Should contain a date-like pattern
            Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", result);
        }

        [Fact]
        public void Write_ContainsCategoryShortName()
        {
            var result = FormatLogEntry(LogLevel.Information, "Test");

            // The category is "TestCategory", and the formatter takes the last segment after '.'
            Assert.Contains("TestCategory", result);
        }

        [Fact]
        public void Write_OutputEndsWithNewline()
        {
            var result = FormatLogEntry(LogLevel.Information, "Test");

            Assert.EndsWith(Environment.NewLine, result);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void Write_AllLogLevels_ProduceOutput(LogLevel logLevel)
        {
            var result = FormatLogEntry(logLevel, "Test message");

            Assert.False(string.IsNullOrWhiteSpace(result));
            Assert.Contains("Test message", result);
        }

        [Fact]
        public void Write_ContainsAnsiColorCodes()
        {
            var result = FormatLogEntry(LogLevel.Information, "Test");

            // ANSI escape code for color reset
            Assert.Contains("\x1B[0m", result);
        }
    }
}
