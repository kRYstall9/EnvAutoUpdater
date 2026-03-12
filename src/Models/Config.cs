using System.Text.Json;

namespace EnvAutoUpdater.src.Models
{
    public class Config
    {
        /// <summary>
        /// Gets or sets the collection of services that are scheduled to be updated.
        /// </summary>
        /// <remarks>If the property is null, no services are currently marked for update. Modifying this
        /// collection affects which services will be processed during the update operation.</remarks>
        public List<ServiceToUpdate>? ServicesToUpdate { get; set; }
        /// <summary>
        /// Gets or sets the interval, in seconds, at which checks are performed.
        /// </summary>
        public int? CheckInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a backup file should be created before updating the .env file
        /// </summary>
        public bool SaveBackupFile { get; set; }

        /// <summary>
        /// Gets or sets the time zone information for logging purposes
        /// </summary>
        public string? TZInfo { get; set; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
