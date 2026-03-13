using EnvAutoUpdater.src.Models;
using Newtonsoft.Json;
using Xunit;

namespace EnvAutoUpdater.Tests.Models
{
    public class ServiceTests
    {
        [Fact]
        public void Service_Deserialization_SetsProperties()
        {
            var json = """
            {
                "name": "MyService",
                "repositoryEnvUrl": "https://github.com/repo/.env",
                "localEnvPath": "/app/.env"
            }
            """;

            var service = JsonConvert.DeserializeObject<Service>(json);

            Assert.NotNull(service);
            Assert.Equal("MyService", service!.ServiceName);
            Assert.Equal("https://github.com/repo/.env", service.EnvRepoUrl);
            Assert.Equal("/app/.env", service.EnvLocalFilePath);
        }

        [Fact]
        public void Service_DefaultValues_AreNull()
        {
            var service = new Service();

            Assert.Null(service.ServiceName);
            Assert.Null(service.EnvRepoUrl);
            Assert.Null(service.EnvLocalFilePath);
        }

        [Fact]
        public void Service_Serialization_UsesJsonPropertyNames()
        {
            var service = new Service
            {
                ServiceName = "test",
                EnvRepoUrl = "https://url",
                EnvLocalFilePath = "/path"
            };

            var json = JsonConvert.SerializeObject(service);

            Assert.Contains("\"name\":", json);
            Assert.Contains("\"repositoryEnvUrl\":", json);
            Assert.Contains("\"localEnvPath\":", json);
        }

        [Fact]
        public void Service_Deserialization_PartialJson_LeavesOthersNull()
        {
            var json = """{ "name": "OnlyName" }""";

            var service = JsonConvert.DeserializeObject<Service>(json);

            Assert.NotNull(service);
            Assert.Equal("OnlyName", service!.ServiceName);
            Assert.Null(service.EnvRepoUrl);
            Assert.Null(service.EnvLocalFilePath);
        }

        [Fact]
        public void Service_Serialization_RoundTrip()
        {
            var service = new Service
            {
                ServiceName = "myapp",
                EnvRepoUrl = "https://github.com/org/repo/blob/main/.env",
                EnvLocalFilePath = "/opt/app/.env"
            };

            var json = JsonConvert.SerializeObject(service);
            var deserialized = JsonConvert.DeserializeObject<Service>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(service.ServiceName, deserialized!.ServiceName);
            Assert.Equal(service.EnvRepoUrl, deserialized.EnvRepoUrl);
            Assert.Equal(service.EnvLocalFilePath, deserialized.EnvLocalFilePath);
        }

        [Fact]
        public void Service_Deserialization_EmptyJson_AllNull()
        {
            var json = "{}";

            var service = JsonConvert.DeserializeObject<Service>(json);

            Assert.NotNull(service);
            Assert.Null(service!.ServiceName);
            Assert.Null(service.EnvRepoUrl);
            Assert.Null(service.EnvLocalFilePath);
        }

        [Fact]
        public void Service_InitOnly_CanBeSetViaInitializer()
        {
            var service = new Service
            {
                ServiceName = "test",
                EnvRepoUrl = "https://url",
                EnvLocalFilePath = "/path"
            };

            Assert.Equal("test", service.ServiceName);
            Assert.Equal("https://url", service.EnvRepoUrl);
            Assert.Equal("/path", service.EnvLocalFilePath);
        }
    }
}
