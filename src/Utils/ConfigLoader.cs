using EnvAutoUpdater.src.Models;
using Newtonsoft.Json;

namespace EnvAutoUpdater.src.Utils
{
    public static class ConfigLoader
    {
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
    }
}
