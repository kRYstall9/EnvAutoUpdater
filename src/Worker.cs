using EnvAutoUpdater.src.Models;
using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnvAutoUpdater.src
{
    public class Worker(ILogger<Worker> _logger, IEnvUpdaterService _envUpdaterService, IConfigService _configService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Config config = await _configService.GetConfig();

            _logger.LogInformation($"Config: {config.ToString()}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await _envUpdaterService.RunCheck();
                var next = DateTime.Now.AddSeconds(config.CheckInterval!.Value);

                _logger.LogInformation($"Next execution: {next}");

                var delay = Math.Max(0, (int)(next - DateTime.Now).TotalMilliseconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
