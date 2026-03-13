using EnvAutoUpdater.src.Models;
using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections;

namespace EnvAutoUpdater.src.Services
{
    public class ConfigService(ILogger<ConfigService> _logger) : IConfigService
    {
        public Task<Config> GetConfig()
        {
            Config? config;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading config file");
                throw;
            }

            return Task.FromResult(config!);
        }
    }
}
