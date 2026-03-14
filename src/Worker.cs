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

            _logger.LogInformation($"Config: {config}");

            while (!stoppingToken.IsCancellationRequested)
            {
                config = ConfigLoader.LoadCached() ?? config;
                await _envUpdaterService.Run(stoppingToken);

                var tz = ConfigLoader.GetTimeZone(config.TZInfo);
                var next = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow.AddSeconds(config.CheckInterval), tz);

                _logger.LogInformation($"Next execution: {next:yyyy-MM-dd HH:mm:ss}");

                var delay = (int)Math.Min(Math.Max(0, (long)(next - DateTimeOffset.UtcNow).TotalMilliseconds), int.MaxValue);

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
