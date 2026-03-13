using EnvAutoUpdater.src.Utils;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EnvAutoUpdater.Tests.Utils
{
    public class LogLevelHelperTests
    {
        [Theory]
        [InlineData("trace", LogLevel.Trace)]
        [InlineData("debug", LogLevel.Debug)]
        [InlineData("info", LogLevel.Information)]
        [InlineData("information", LogLevel.Information)]
        [InlineData("warn", LogLevel.Warning)]
        [InlineData("warning", LogLevel.Warning)]
        [InlineData("error", LogLevel.Error)]
        [InlineData("critical", LogLevel.Critical)]
        [InlineData("crit", LogLevel.Critical)]
        [InlineData("none", LogLevel.None)]
        public void Parse_ValidValues_ReturnsExpectedLogLevel(string input, LogLevel expected)
        {
            var result = LogLevelHelper.Parse(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TRACE", LogLevel.Trace)]
        [InlineData("DEBUG", LogLevel.Debug)]
        [InlineData("Info", LogLevel.Information)]
        [InlineData("WARNING", LogLevel.Warning)]
        [InlineData("Error", LogLevel.Error)]
        public void Parse_CaseInsensitive_ReturnsExpectedLogLevel(string input, LogLevel expected)
        {
            var result = LogLevelHelper.Parse(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_NullValue_ReturnsFallback()
        {
            var result = LogLevelHelper.Parse(null);
            Assert.Equal(LogLevel.Information, result);
        }

        [Fact]
        public void Parse_EmptyString_ReturnsFallback()
        {
            var result = LogLevelHelper.Parse("");
            Assert.Equal(LogLevel.Information, result);
        }

        [Fact]
        public void Parse_WhitespaceString_ReturnsFallback()
        {
            var result = LogLevelHelper.Parse("   ");
            Assert.Equal(LogLevel.Information, result);
        }

        [Fact]
        public void Parse_InvalidValue_ReturnsFallback()
        {
            var result = LogLevelHelper.Parse("notavalidlevel");
            Assert.Equal(LogLevel.Information, result);
        }

        [Fact]
        public void Parse_InvalidValue_ReturnsCustomFallback()
        {
            var result = LogLevelHelper.Parse("invalid", LogLevel.Error);
            Assert.Equal(LogLevel.Error, result);
        }

        [Fact]
        public void Parse_NullValue_ReturnsCustomFallback()
        {
            var result = LogLevelHelper.Parse(null, LogLevel.Debug);
            Assert.Equal(LogLevel.Debug, result);
        }

        [Theory]
        [InlineData("Trace", LogLevel.Trace)]
        [InlineData("Information", LogLevel.Information)]
        [InlineData("Warning", LogLevel.Warning)]
        [InlineData("Critical", LogLevel.Critical)]
        [InlineData("None", LogLevel.None)]
        public void Parse_DotNetEnumNames_ReturnsExpectedLogLevel(string input, LogLevel expected)
        {
            var result = LogLevelHelper.Parse(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("CRIT", LogLevel.Critical)]
        [InlineData("WARN", LogLevel.Warning)]
        [InlineData("INFO", LogLevel.Information)]
        public void Parse_UppercaseAliases_ReturnsExpectedLogLevel(string input, LogLevel expected)
        {
            var result = LogLevelHelper.Parse(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Parse_TabString_ReturnsFallback()
        {
            var result = LogLevelHelper.Parse("\t");
            Assert.Equal(LogLevel.Information, result);
        }

        [Fact]
        public void Parse_AllFallbackLevels_Work()
        {
            foreach (LogLevel level in Enum.GetValues<LogLevel>())
            {
                var result = LogLevelHelper.Parse("invalid_value", level);
                Assert.Equal(level, result);
            }
        }
    }
}
