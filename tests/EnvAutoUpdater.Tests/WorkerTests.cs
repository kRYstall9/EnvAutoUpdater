using EnvAutoUpdater.src;
using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnvAutoUpdater.Tests
{
    public class WorkerTests
    {
        [Fact]
        public async Task ExecuteAsync_CancellationRequested_StopsGracefully()
        {
            var loggerMock = new Mock<ILogger<Worker>>();
            var envUpdaterMock = new Mock<IEnvUpdaterService>();

            var worker = new Worker(loggerMock.Object, envUpdaterMock.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // StartAsync internally calls ExecuteAsync; with token already cancelled it should exit gracefully
            await worker.StartAsync(cts.Token);

            // Give a small window for the background task to complete
            await Task.Delay(100);

            await worker.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidConfig_CallsRunAtLeastOnce()
        {
            var loggerMock = new Mock<ILogger<Worker>>();
            var envUpdaterMock = new Mock<IEnvUpdaterService>();

            // Create a temp config.json in the working directory so ConfigLoader.Load() succeeds
            var configJson = """
            {
                "services": [],
                "checkInterval": 1,
                "logLevel": "info"
            }
            """;
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            var configExisted = File.Exists(configPath);
            string? originalContent = configExisted ? await File.ReadAllTextAsync(configPath) : null;

            try
            {
                await File.WriteAllTextAsync(configPath, configJson);

                var worker = new Worker(loggerMock.Object, envUpdaterMock.Object);

                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

                await worker.StartAsync(cts.Token);
                await Task.Delay(600);

                try { await worker.StopAsync(CancellationToken.None); }
                catch (OperationCanceledException) { }

                envUpdaterMock.Verify(x => x.Run(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            }
            finally
            {
                if (configExisted && originalContent != null)
                    await File.WriteAllTextAsync(configPath, originalContent);
                else if (!configExisted && File.Exists(configPath))
                    File.Delete(configPath);
            }
        }

        [Fact]
        public async Task ExecuteAsync_WithNullConfig_LogsErrorAndReturns()
        {
            var loggerMock = new Mock<ILogger<Worker>>();
            var envUpdaterMock = new Mock<IEnvUpdaterService>();

            // Ensure no config.json exists so ConfigLoader.Load() returns null
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            var configExisted = File.Exists(configPath);
            string? originalContent = configExisted ? await File.ReadAllTextAsync(configPath) : null;

            try
            {
                if (File.Exists(configPath))
                    File.Delete(configPath);

                var worker = new Worker(loggerMock.Object, envUpdaterMock.Object);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                await worker.StartAsync(cts.Token);
                await Task.Delay(200);
                await worker.StopAsync(CancellationToken.None);

                // Run should never be called since config is null
                envUpdaterMock.Verify(x => x.Run(It.IsAny<CancellationToken>()), Times.Never());
            }
            finally
            {
                if (configExisted && originalContent != null)
                    await File.WriteAllTextAsync(configPath, originalContent);
            }
        }
    }
}
