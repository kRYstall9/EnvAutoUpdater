using EnvAutoUpdater.src.Models;
using Newtonsoft.Json;
using Xunit;

namespace EnvAutoUpdater.Tests.Models
{
    public class ConfigTests
    {
        [Fact]
        public void Config_DefaultValues_AreCorrect()
        {
            var config = new Config();

            Assert.Empty(config.Services!);
            Assert.Equal(3600, config.CheckInterval);
            Assert.True(config.SaveBackupFile);
            Assert.Equal("UTC", config.TZInfo);
            Assert.Equal("info", config.LogLevel);
        }

        [Fact]
        public void Config_Deserialization_SetsProperties()
        {
            var json = """
            {
                "services": [{ "name": "svc1", "repositoryEnvUrl": "https://url/.env", "localEnvPath": "/p/.env" }],
                "checkInterval": 120,
                "saveBackupFile": false,
                "timezone": "America/New_York",
                "logLevel": "debug"
            }
            """;

            var config = JsonConvert.DeserializeObject<Config>(json);

            Assert.NotNull(config);
            Assert.Single(config!.Services!);
            Assert.Equal(120, config.CheckInterval);
            Assert.False(config.SaveBackupFile);
            Assert.Equal("America/New_York", config.TZInfo);
            Assert.Equal("debug", config.LogLevel);
        }

        [Fact]
        public void Config_ToString_ReturnsValidJson()
        {
            var config = new Config { CheckInterval = 500, LogLevel = "warn" };

            var result = config.ToString();

            Assert.Contains("500", result);
            Assert.Contains("warn", result);
            // Should be valid JSON
            var deserialized = JsonConvert.DeserializeObject<Config>(result);
            Assert.NotNull(deserialized);
            Assert.Equal(500, deserialized!.CheckInterval);
        }

        [Fact]
        public void Config_ToString_IncludesAllProperties()
        {
            var config = new Config
            {
                Services = [new Service { ServiceName = "svc1" }],
                CheckInterval = 300,
                SaveBackupFile = false,
                TZInfo = "Europe/Rome",
                LogLevel = "debug"
            };

            var result = config.ToString();

            Assert.Contains("svc1", result);
            Assert.Contains("300", result);
            Assert.Contains("false", result.ToLower());
            Assert.Contains("Europe/Rome", result);
            Assert.Contains("debug", result);
        }

        [Fact]
        public void Config_Deserialization_PartialJson_UsesDefaults()
        {
            var json = """{ "checkInterval": 60 }""";

            var config = JsonConvert.DeserializeObject<Config>(json);

            Assert.NotNull(config);
            Assert.Equal(60, config!.CheckInterval);
            Assert.True(config.SaveBackupFile); // default
            Assert.Equal("UTC", config.TZInfo); // default
            Assert.Equal("info", config.LogLevel); // default
        }

        [Fact]
        public void Config_Services_CanBeSetToNull()
        {
            var json = """{ "services": null }""";

            var config = JsonConvert.DeserializeObject<Config>(json);

            Assert.NotNull(config);
            Assert.Null(config!.Services);
        }

        [Fact]
        public void Config_Serialization_RoundTrip()
        {
            var config = new Config
            {
                Services = [new Service { ServiceName = "test", EnvRepoUrl = "https://url", EnvLocalFilePath = "/path" }],
                CheckInterval = 120,
                SaveBackupFile = false,
                TZInfo = "US/Pacific",
                LogLevel = "error"
            };

            var json = config.ToString();
            var deserialized = JsonConvert.DeserializeObject<Config>(json);

            Assert.NotNull(deserialized);
            Assert.Single(deserialized!.Services!);
            Assert.Equal("test", deserialized.Services![0].ServiceName);
            Assert.Equal(120, deserialized.CheckInterval);
            Assert.False(deserialized.SaveBackupFile);
            Assert.Equal("US/Pacific", deserialized.TZInfo);
            Assert.Equal("error", deserialized.LogLevel);
        }
    }
}
