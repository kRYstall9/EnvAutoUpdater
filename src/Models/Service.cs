using Newtonsoft.Json;

namespace EnvAutoUpdater.src.Models
{
    public class Service
    {
        /// <summary>
        /// Gets or sets the name of the service associated with the environment file to be updated
        /// </summary>

        [JsonProperty("name")]
        public string? ServiceName { get; init; }
        /// <summary>
        /// Gets or sets the URL of the repository where the updated .env file can be found
        /// </summary>
        [JsonProperty("repositoryEnvUrl")]
        public string? EnvRepoUrl { get; init; }
        /// <summary>
        /// Gets or sets the file path to the local environment configuration file.
        /// </summary>
        [JsonProperty("localEnvPath")]
        public string? EnvLocalFilePath { get; init; }
    }
}
