using EnvAutoUpdater.src.Models;
using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace EnvAutoUpdater.src.Services
{
    public class ConfigService(ILogger<ConfigService> _logger) : IConfigService
    {
        private const string _repoUrlSuffix = "_ENV_REPO_URL";
        private const string _envPathSuffix = "_ENV_PATH";
        private Config? Config;

        public Task<Config> GetConfig()
        {

            if(Config != null)
            {
                _logger.LogDebug("Returning cached config.");
                return Task.FromResult(Config);
            }

            this.Config = new Config();

            var envVars = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .ToDictionary(
                    e => e.Key.ToString()!,
                    e => e.Value?.ToString()! ?? string.Empty
                );

            var services = envVars.Keys
                .Where(k => k.EndsWith(_repoUrlSuffix) || k.EndsWith(_envPathSuffix))
                .Select(k => k.EndsWith(_repoUrlSuffix) ? k[..^_repoUrlSuffix.Length] : k[..^_envPathSuffix.Length])
                .Distinct()
                .Select(prefix =>
                {
                    _logger.LogDebug($"Reading config for the service with prefix: {prefix}");

                    var envPath = envVars.GetValueOrDefault($"{prefix}{_envPathSuffix}");
                    var repoUrl = envVars.GetValueOrDefault($"{prefix}{_repoUrlSuffix}");

                    _logger.LogDebug($"{prefix}\'s env path: {envPath} | {prefix}\'s repo url: {repoUrl}");

                    if (string.IsNullOrEmpty(envPath) || string.IsNullOrEmpty(repoUrl))
                    {
                        _logger.LogWarning($"Skipping {prefix} due to missing environment variables.");
                        return null;
                    }

                    return new ServiceToUpdate
                    {
                        ServiceName = prefix,
                        EnvRepoUrl = repoUrl,
                        EnvLocalFilePath = envPath
                    };
                })
                .Where(s => s is not null)
                .Cast<ServiceToUpdate>()
                .ToList();

            this.Config.ServicesToUpdate = services;
            this.Config.CheckInterval = int.TryParse(envVars.GetValueOrDefault("CHECK_INTERVAL"), out int checkInterval) ? checkInterval : 3600;

            return Task.FromResult(this.Config);
        }
    }
}
