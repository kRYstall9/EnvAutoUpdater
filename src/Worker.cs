using EnvAutoUpdater.src.Models;
using EnvAutoUpdater.src.Services.Interfaces;
using EnvAutoUpdater.src.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnvAutoUpdater.src
{
    public class Worker(ILogger<Worker> _logger, IEnvUpdaterService _envUpdaterService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Config? config = await ConfigLoader.Load();

            if (config == null)
            {
                _logger.LogError("Failed to load configuration. Exiting.");
                return;
            }

            _logger.LogInformation($"Config: {config!.ToString()}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await _envUpdaterService.Run();
                var next = DateTime.Now.AddSeconds(config.CheckInterval);

                _logger.LogInformation($"Next execution: {next}");

                var delay = Math.Max(0, (int)(next - DateTime.Now).TotalMilliseconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
