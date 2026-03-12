namespace EnvAutoUpdater.src.Models
{
    public class ServiceToUpdate
    {
        /// <summary>
        /// Gets or sets the name of the service associated with the environment file to be updated
        /// </summary>
        public string? ServiceName { get; init; }
        /// <summary>
        /// Gets or sets the URL of the repository where the updated .env file can be found
        /// </summary>
        public string? EnvRepoUrl { get; init; }
        /// <summary>
        /// Gets or sets the file path to the local environment configuration file.
        /// </summary>
        public string? EnvLocalFilePath { get; init; }
    }
}
