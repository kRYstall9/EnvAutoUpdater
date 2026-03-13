using Newtonsoft.Json;
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
        [JsonProperty("services")]
        public List<Service>? Services { get; set; } = [];
        /// <summary>
        /// Gets or sets the interval, in seconds, at which checks are performed.
        /// </summary>
        [JsonProperty("checkInterval")]
        public int CheckInterval { get; set; } = 3600;

        /// <summary>
        /// Gets or sets a value indicating whether a backup file should be created before updating the .env file
        /// </summary>
        [JsonProperty("saveBackupFile")]
        public bool SaveBackupFile { get; set; } = true;

        /// <summary>
        /// Gets or sets the time zone information for logging purposes
        /// </summary>
        [JsonProperty("timezone")]
        public string? TZInfo { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the log level used for output messages.
        /// </summary>
        /// <remarks>Valid values typically include "info", "debug", "warn", and "error". The log level
        /// determines the minimum severity of messages that will be recorded or displayed.</remarks>
        [JsonProperty("logLevel")]
        public string? LogLevel { get; set; } = "info";
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
