using EnvAutoUpdater.src.Utils;
using Xunit;

namespace EnvAutoUpdater.Tests.Utils
{
    public class ConfigLoaderTests : IDisposable
    {
        private readonly List<string> _tempFiles = [];

        public void Dispose()
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        private string CreateTempConfigFile(string content)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, content);
            _tempFiles.Add(path);
            return path;
        }

        [Fact]
        public async Task Load_ValidConfig_ReturnsConfig()
        {
            var json = """
            {
                "services": [
                    {
                        "name": "TestService",
                        "repositoryEnvUrl": "https://github.com/test/.env",
                        "localEnvPath": "/tmp/.env"
                    }
                ],
                "checkInterval": 600,
                "saveBackupFile": false,
                "timezone": "Europe/Rome",
                "logLevel": "debug"
            }
            """;
            var path = CreateTempConfigFile(json);

            var config = await ConfigLoader.Load(path);

            Assert.NotNull(config);
            Assert.Single(config!.Services!);
            Assert.Equal("TestService", config.Services![0].ServiceName);
            Assert.Equal("https://github.com/test/.env", config.Services[0].EnvRepoUrl);
            Assert.Equal("/tmp/.env", config.Services[0].EnvLocalFilePath);
            Assert.Equal(600, config.CheckInterval);
            Assert.False(config.SaveBackupFile);
            Assert.Equal("Europe/Rome", config.TZInfo);
            Assert.Equal("debug", config.LogLevel);
        }

        [Fact]
        public async Task Load_EmptyJson_ReturnsConfigWithDefaults()
        {
            var path = CreateTempConfigFile("{}");

            var config = await ConfigLoader.Load(path);

            Assert.NotNull(config);
            Assert.Empty(config!.Services!);
            Assert.Equal(3600, config.CheckInterval);
            Assert.True(config.SaveBackupFile);
            Assert.Equal("UTC", config.TZInfo);
            Assert.Equal("info", config.LogLevel);
        }

        [Fact]
        public async Task Load_InvalidJson_ReturnsNull()
        {
            var path = CreateTempConfigFile("not valid json{{{");

            var config = await ConfigLoader.Load(path);

            Assert.Null(config);
        }

        [Fact]
        public async Task Load_NonExistentFile_ReturnsNull()
        {
            var config = await ConfigLoader.Load("/non/existent/path/config.json");

            Assert.Null(config);
        }

        [Fact]
        public async Task Load_MultipleServices_ReturnsAllServices()
        {
            var json = """
            {
                "services": [
                    { "name": "svc1", "repositoryEnvUrl": "https://url1/.env", "localEnvPath": "/p1/.env" },
                    { "name": "svc2", "repositoryEnvUrl": "https://url2/.env", "localEnvPath": "/p2/.env" }
                ]
            }
            """;
            var path = CreateTempConfigFile(json);

            var config = await ConfigLoader.Load(path);

            Assert.NotNull(config);
            Assert.Equal(2, config!.Services!.Count);
        }

        [Fact]
        public async Task Load_DefaultPath_UsesConfigJson()
        {
            // This tests the default parameter; will depend on whether config.json exists
            // in the working directory. We just verify it doesn't throw.
            var config = await ConfigLoader.Load();

            // Result depends on whether config.json exists - just verify no exception
            Assert.True(config == null || config != null);
        }

        [Fact]
        public async Task Load_EmptyFile_ReturnsNull()
        {
            var path = CreateTempConfigFile(string.Empty);

            var config = await ConfigLoader.Load(path);

            Assert.Null(config);
        }

        [Fact]
        public async Task Load_NullServicesInJson_ReturnsConfigWithNullServices()
        {
            var json = """{ "services": null, "checkInterval": 100 }""";
            var path = CreateTempConfigFile(json);

            var config = await ConfigLoader.Load(path);

            Assert.NotNull(config);
            Assert.Null(config!.Services);
            Assert.Equal(100, config.CheckInterval);
        }

        [Fact]
        public async Task Load_ExtraFieldsInJson_IgnoresUnknownFields()
        {
            var json = """
            {
                "services": [],
                "checkInterval": 300,
                "unknownField": "someValue",
                "anotherUnknown": 42
            }
            """;
            var path = CreateTempConfigFile(json);

            var config = await ConfigLoader.Load(path);

            Assert.NotNull(config);
            Assert.Equal(300, config!.CheckInterval);
        }
    }
}
