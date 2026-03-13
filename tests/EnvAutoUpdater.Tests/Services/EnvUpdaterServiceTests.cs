using EnvAutoUpdater.src.Services;
using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace EnvAutoUpdater.Tests.Services
{
    public class EnvUpdaterServiceTests : IDisposable
    {
        private readonly Mock<ILogger<IEnvUpdaterService>> _loggerMock;
        private readonly EnvUpdaterService _service;
        private readonly List<string> _tempFiles = [];

        public EnvUpdaterServiceTests()
        {
            _loggerMock = new Mock<ILogger<IEnvUpdaterService>>();
            _service = new EnvUpdaterService(_loggerMock.Object, new HttpClient());
        }

        public void Dispose()
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        private string CreateTempFile(string content)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, content);
            _tempFiles.Add(path);
            return path;
        }

        private static EnvUpdaterService CreateServiceWithMockHttp(
            Mock<ILogger<IEnvUpdaterService>> loggerMock,
            HttpStatusCode statusCode,
            string responseContent)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            return new EnvUpdaterService(loggerMock.Object, httpClient);
        }

        #region ReadEnvVariables Tests

        [Fact]
        public async Task ReadEnvVariables_SimpleVariables_ReturnsNames()
        {
            string[] lines = ["DB_HOST=localhost", "DB_PORT=5432", "DB_NAME=mydb"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Equal(3, result.Count);
            Assert.Contains("DB_HOST", result);
            Assert.Contains("DB_PORT", result);
            Assert.Contains("DB_NAME", result);
        }

        [Fact]
        public async Task ReadEnvVariables_EmptyLines_AreSkipped()
        {
            string[] lines = ["DB_HOST=localhost", "", "   ", "DB_PORT=5432"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Equal(2, result.Count);
            Assert.Contains("DB_HOST", result);
            Assert.Contains("DB_PORT", result);
        }

        [Fact]
        public async Task ReadEnvVariables_CommentedVariables_ExtractVarName()
        {
            string[] lines = ["#DB_HOST=localhost", "DB_PORT=5432"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Equal(2, result.Count);
            Assert.Contains("DB_HOST", result);
            Assert.Contains("DB_PORT", result);
        }

        [Fact]
        public async Task ReadEnvVariables_DuplicateVariables_ReturnsDistinct()
        {
            string[] lines = ["DB_HOST=localhost", "DB_HOST=remotehost"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Single(result);
            Assert.Contains("DB_HOST", result);
        }

        [Fact]
        public async Task ReadEnvVariables_LinesWithoutEquals_AreSkipped()
        {
            string[] lines = ["# This is a comment", "DB_HOST=localhost", "just some text"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Single(result);
            Assert.Contains("DB_HOST", result);
        }

        [Fact]
        public async Task ReadEnvVariables_EmptyArray_ReturnsEmpty()
        {
            string[] lines = [];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReadEnvVariables_VarNameWithSpaces_IsSkipped()
        {
            string[] lines = ["VALID_VAR=value", "not a var name=value"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Single(result);
            Assert.Contains("VALID_VAR", result);
        }

        [Fact]
        public async Task ReadEnvVariables_VarWithComplexValue_ExtractsName()
        {
            string[] lines = ["CONNECTION_STRING=Server=localhost;Port=5432;Database=mydb"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Single(result);
            Assert.Contains("CONNECTION_STRING", result);
        }

        [Fact]
        public async Task ReadEnvVariables_CommentedVarWithSpaces_ExtractsName()
        {
            string[] lines = ["# DB_HOST=localhost"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Single(result);
            Assert.Contains("DB_HOST", result);
        }

        [Fact]
        public async Task ReadEnvVariables_OnlyComments_ReturnsEmpty()
        {
            string[] lines = ["# This is a comment", "# Another comment"];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReadEnvVariables_MixedContent_ReturnsCorrectVars()
        {
            string[] lines =
            [
                "# Database configuration",
                "DB_HOST=localhost",
                "",
                "# Port",
                "DB_PORT=5432",
                "# Commented out for now",
                "#REDIS_URL=redis://localhost:6379",
                "",
                "API_KEY=abc123"
            ];

            var result = await _service.ReadEnvVariables(lines);

            Assert.Equal(4, result.Count);
            Assert.Contains("DB_HOST", result);
            Assert.Contains("DB_PORT", result);
            Assert.Contains("REDIS_URL", result);
            Assert.Contains("API_KEY", result);
        }

        #endregion

        #region IsFileAlreadyUpdated Tests

        [Fact]
        public async Task IsFileAlreadyUpdated_AllRepoVarsInLocal_ReturnsTrue()
        {
            var localVars = new List<string> { "DB_HOST", "DB_PORT", "DB_NAME" };
            var repoVars = new List<string> { "DB_HOST", "DB_PORT" };

            var result = await _service.IsFileAlreadyUpdated(localVars, repoVars);

            Assert.True(result);
        }

        [Fact]
        public async Task IsFileAlreadyUpdated_MissingRepoVarInLocal_ReturnsFalse()
        {
            var localVars = new List<string> { "DB_HOST" };
            var repoVars = new List<string> { "DB_HOST", "NEW_VAR" };

            var result = await _service.IsFileAlreadyUpdated(localVars, repoVars);

            Assert.False(result);
        }

        [Fact]
        public async Task IsFileAlreadyUpdated_EmptyRepoVars_ReturnsTrue()
        {
            var localVars = new List<string> { "DB_HOST" };
            var repoVars = new List<string>();

            var result = await _service.IsFileAlreadyUpdated(localVars, repoVars);

            Assert.True(result);
        }

        [Fact]
        public async Task IsFileAlreadyUpdated_BothEmpty_ReturnsTrue()
        {
            var result = await _service.IsFileAlreadyUpdated([], []);

            Assert.True(result);
        }

        [Fact]
        public async Task IsFileAlreadyUpdated_ExactMatch_ReturnsTrue()
        {
            var vars = new List<string> { "A", "B", "C" };

            var result = await _service.IsFileAlreadyUpdated(vars, new List<string>(vars));

            Assert.True(result);
        }

        [Fact]
        public async Task IsFileAlreadyUpdated_LocalEmpty_RepoHasVars_ReturnsFalse()
        {
            var localVars = new List<string>();
            var repoVars = new List<string> { "NEW_VAR" };

            var result = await _service.IsFileAlreadyUpdated(localVars, repoVars);

            Assert.False(result);
        }

        [Fact]
        public async Task IsFileAlreadyUpdated_MultipleNewVars_ReturnsFalse()
        {
            var localVars = new List<string> { "A" };
            var repoVars = new List<string> { "A", "B", "C", "D" };

            var result = await _service.IsFileAlreadyUpdated(localVars, repoVars);

            Assert.False(result);
        }

        #endregion

        #region ReadLocalEnvFile Tests

        [Fact]
        public async Task ReadLocalEnvFile_ValidFile_ReturnsContent()
        {
            var path = CreateTempFile("DB_HOST=localhost\nDB_PORT=5432");

            var result = await _service.ReadLocalEnvFile(path);

            Assert.NotEmpty(result);
            Assert.Contains(result, line => line.Contains("DB_HOST"));
        }

        [Fact]
        public async Task ReadLocalEnvFile_NonExistentFile_ReturnsEmptyArray()
        {
            var result = await _service.ReadLocalEnvFile("/non/existent/file.env");

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReadLocalEnvFile_EmptyFile_ReturnsEmptyLines()
        {
            var path = CreateTempFile("");

            var result = await _service.ReadLocalEnvFile(path);

            // File.ReadAllLinesAsync on an empty file returns a single-element array with an empty string or empty array
            Assert.True(result.Length <= 1);
        }

        [Fact]
        public async Task ReadLocalEnvFile_MultipleLines_ReturnsAllLines()
        {
            var content = "LINE1=val1\nLINE2=val2\nLINE3=val3";
            var path = CreateTempFile(content);

            var result = await _service.ReadLocalEnvFile(path);

            Assert.Equal(3, result.Length);
        }

        [Fact]
        public async Task ReadLocalEnvFile_CancellationRequested_ReturnsEmpty()
        {
            var path = CreateTempFile("DB_HOST=localhost");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await _service.ReadLocalEnvFile(path, cts.Token);

            Assert.Empty(result);
        }

        #endregion

        #region UpdateEnvFile Tests

        [Fact]
        public async Task UpdateEnvFile_NewVarAdded_AppearsInResult()
        {
            var localVars = new List<string> { "DB_HOST" };
            var repoVars = new List<string> { "DB_HOST", "NEW_VAR" };
            string[] localContent = ["DB_HOST=localhost"];
            string[] repoContent = ["DB_HOST=localhost", "NEW_VAR=newvalue"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("NEW_VAR")));
        }

        [Fact]
        public async Task UpdateEnvFile_NoNewVars_KeepsOriginal()
        {
            var vars = new List<string> { "DB_HOST" };
            string[] localContent = ["DB_HOST=localhost"];
            string[] repoContent = ["DB_HOST=localhost"];

            var result = await _service.UpdateEnvFile(vars, vars, localContent, repoContent);

            Assert.Contains("DB_HOST=localhost", result);
        }

        [Fact]
        public async Task UpdateEnvFile_MarkerIsAdded()
        {
            var localVars = new List<string> { "DB_HOST" };
            var repoVars = new List<string> { "DB_HOST", "NEW_VAR" };
            string[] localContent = ["DB_HOST=localhost"];
            string[] repoContent = ["DB_HOST=localhost", "NEW_VAR=value"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("EnvAutoUpdater")));
        }

        [Fact]
        public async Task UpdateEnvFile_NewVarIsCommentedOut()
        {
            var localVars = new List<string> { "DB_HOST" };
            var repoVars = new List<string> { "DB_HOST", "NEW_VAR" };
            string[] localContent = ["DB_HOST=localhost"];
            string[] repoContent = ["DB_HOST=localhost", "NEW_VAR=value"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("# NEW_VAR=value")));
        }

        [Fact]
        public async Task UpdateEnvFile_CommentsFromRepoArePreserved()
        {
            var localVars = new List<string> { "DB_HOST" };
            var repoVars = new List<string> { "DB_HOST", "NEW_VAR" };
            string[] localContent = ["DB_HOST=localhost"];
            string[] repoContent = ["DB_HOST=localhost", "# Description of NEW_VAR", "NEW_VAR=value"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("# Description of NEW_VAR")));
        }

        [Fact]
        public async Task UpdateEnvFile_MultipleNewVars_AllAdded()
        {
            var localVars = new List<string> { "A" };
            var repoVars = new List<string> { "A", "B", "C" };
            string[] localContent = ["A=1"];
            string[] repoContent = ["A=1", "B=2", "C=3"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("B")));
            Assert.True(result.Any(line => line.Contains("C")));
        }

        [Fact]
        public async Task UpdateEnvFile_EmptyLocalContent_AddsAllRepoVars()
        {
            var localVars = new List<string>();
            var repoVars = new List<string> { "NEW_A", "NEW_B" };
            string[] localContent = [];
            string[] repoContent = ["NEW_A=1", "NEW_B=2"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("NEW_A")));
            Assert.True(result.Any(line => line.Contains("NEW_B")));
        }

        [Fact]
        public async Task UpdateEnvFile_MarkerNotDuplicated()
        {
            var localVars = new List<string> { "A" };
            var repoVars = new List<string> { "A", "B" };
            string marker = "#-------------- This section was automatically updated by EnvAutoUpdater -----------";
            string[] localContent = ["A=1", marker];
            string[] repoContent = ["A=1", "B=2"];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            var markerCount = result.Count(line => line.Contains("EnvAutoUpdater"));
            Assert.Equal(1, markerCount);
        }

        [Fact]
        public async Task UpdateEnvFile_MultiLineRepoVar_AllLinesCommented()
        {
            var localVars = new List<string> { "A" };
            var repoVars = new List<string> { "A", "JSON_CONFIG" };
            string[] localContent = ["A=1"];
            string[] repoContent =
            [
                "A=1",
                "JSON_CONFIG={",
                "  \"key\": \"value\"",
                "}"
            ];

            var result = await _service.UpdateEnvFile(localVars, repoVars, localContent, repoContent);

            Assert.True(result.Any(line => line.Contains("# JSON_CONFIG={")));
        }

        #endregion

        #region ReadUpdatedEnvFileFromRepository Tests

        [Fact]
        public async Task ReadUpdatedEnvFileFromRepository_InvalidUrlFormat_ReturnsNull()
        {
            var result = await _service.ReadUpdatedEnvFileFromRepository("https://example.com/noenvfile");

            Assert.Null(result);
        }

        [Fact]
        public async Task ReadUpdatedEnvFileFromRepository_RawUrl_MakesRequest()
        {
            var mockService = CreateServiceWithMockHttp(
                _loggerMock,
                HttpStatusCode.OK,
                "DB_HOST=localhost\nDB_PORT=5432");

            var result = await mockService.ReadUpdatedEnvFileFromRepository("https://raw.githubusercontent.com/user/repo/main/.env");

            Assert.NotNull(result);
            Assert.Contains("DB_HOST", result);
        }

        [Fact]
        public async Task ReadUpdatedEnvFileFromRepository_GithubBlobUrl_ConvertedToRaw()
        {
            var mockService = CreateServiceWithMockHttp(
                _loggerMock,
                HttpStatusCode.OK,
                "API_KEY=test");

            var result = await mockService.ReadUpdatedEnvFileFromRepository("https://github.com/user/repo/blob/main/.env");

            Assert.NotNull(result);
            Assert.Contains("API_KEY", result);
        }

        [Fact]
        public async Task ReadUpdatedEnvFileFromRepository_HttpError_ReturnsNull()
        {
            var mockService = CreateServiceWithMockHttp(
                _loggerMock,
                HttpStatusCode.NotFound,
                "");

            var result = await mockService.ReadUpdatedEnvFileFromRepository("https://raw.githubusercontent.com/user/repo/main/.env");

            Assert.Null(result);
        }

        [Fact]
        public async Task ReadUpdatedEnvFileFromRepository_ServerError_ReturnsNull()
        {
            var mockService = CreateServiceWithMockHttp(
                _loggerMock,
                HttpStatusCode.InternalServerError,
                "");

            var result = await mockService.ReadUpdatedEnvFileFromRepository("https://raw.githubusercontent.com/user/repo/main/.env");

            Assert.Null(result);
        }

        [Fact]
        public async Task ReadUpdatedEnvFileFromRepository_EmptyResponse_ReturnsEmptyString()
        {
            var mockService = CreateServiceWithMockHttp(
                _loggerMock,
                HttpStatusCode.OK,
                "");

            var result = await mockService.ReadUpdatedEnvFileFromRepository("https://raw.githubusercontent.com/user/repo/main/.env");

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Run Integration Tests

        [Fact]
        public async Task Run_NoConfigFile_DoesNotThrow()
        {
            // Ensure no config.json in current dir
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            var configExisted = File.Exists(configPath);
            string? originalContent = configExisted ? await File.ReadAllTextAsync(configPath) : null;

            try
            {
                if (File.Exists(configPath))
                    File.Delete(configPath);

                await _service.Run();
                // Should not throw, just log warning
            }
            finally
            {
                if (configExisted && originalContent != null)
                    await File.WriteAllTextAsync(configPath, originalContent);
            }
        }

        [Fact]
        public async Task Run_EmptyServicesList_DoesNotThrow()
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            var configExisted = File.Exists(configPath);
            string? originalContent = configExisted ? await File.ReadAllTextAsync(configPath) : null;

            try
            {
                var json = """{ "services": [] }""";
                await File.WriteAllTextAsync(configPath, json);

                await _service.Run();
                // Should not throw, just log warning about no services
            }
            finally
            {
                if (configExisted && originalContent != null)
                    await File.WriteAllTextAsync(configPath, originalContent);
                else if (!configExisted && File.Exists(configPath))
                    File.Delete(configPath);
            }
        }

        #endregion
    }
}
