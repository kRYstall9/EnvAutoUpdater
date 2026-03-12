using EnvAutoUpdater.src.Models;

namespace EnvAutoUpdater.src.Services.Interfaces
{
    public interface IConfigService
    {

        /// <summary>
        /// Asynchronously retrieves the current configuration settings.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Config"/> object
        /// with the configuration settings.</returns>
        Task<Config> GetConfig();
    }
}
