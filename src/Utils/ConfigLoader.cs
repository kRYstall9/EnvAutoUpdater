using EnvAutoUpdater.src.Models;
using Newtonsoft.Json;

namespace EnvAutoUpdater.src.Utils
{
    public static class ConfigLoader
    {
        private static Config? _cachedConfig;
        private static DateTime _lastLoadTime = DateTime.MinValue;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);
        private static readonly Lock _lock = new();

        public static async Task<Config?> Load(string path = "config.json")
        {
            try
            {
                return JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                return null;
            }
        }

        public static Config? LoadCached(string path = "config.json")
        {
            lock (_lock)
            {
                if (_cachedConfig != null && DateTime.UtcNow - _lastLoadTime < _cacheDuration)
                    return _cachedConfig;
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<Config>(json);

                lock (_lock)
                {
                    _cachedConfig = config;
                    _lastLoadTime = DateTime.UtcNow;
                }

                return config;
            }
            catch
            {
                lock (_lock)
                {
                    return _cachedConfig;
                }
            }
        }
    }
}
