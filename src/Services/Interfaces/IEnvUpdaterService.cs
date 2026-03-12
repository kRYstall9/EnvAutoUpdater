namespace EnvAutoUpdater.src.Services.Interfaces
{
    public interface IEnvUpdaterService
    {
        /// <summary>
        /// Checks and updates the environment file asynchronously if needed.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the update operation. Optional; defaults to none.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task RunCheck(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the contents of a local environment file asynchronously.
        /// </summary>
        /// <param name="envFilePath">The full path to the environment file to read. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the contents of the
        /// environment file as an array of strings, or an empty array if the file does not exist or any error occurs.</returns>
        Task<string[]> ReadLocalEnvFile(string envFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the updated .env file from the specified repository URL.
        /// </summary>
        /// <param name="repoUrl">The URL of the repository where the .env should be read from.The URL could also include the specific path to the .env file within the repository. For example, if the .env file is located in a subdirectory called "config" within the repository, the URL might look like: https://repository-url.com/subdir/config/.env. If the .env file is located at the root of the repository, the URL would simply be: https://repository-url.com/.env. </param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content of the .env file, read from the repository, as a string, or null if the file doesn't exist or is not found</returns>
        Task<string?> ReadUpdatedEnvFileFromRepository(string repoUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether the local environment file content has already been updated to match the specified
        /// updated content.
        /// </summary>
        /// <param name="localEnvVars">The current content of the local environment file to compare.</param>
        /// <param name="repoEnvVars">The content representing the desired updated state of the environment file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the local
        /// environment file content matches the updated content; otherwise, <see langword="false"/>.</returns>
        Task<bool> IsFileAlreadyUpdated (List<string> localEnvVars, List<string> repoEnvVars);
        
        /// <summary>
        /// Extracts environment variable names from the specified lines of a file.
        /// </summary>
        /// <remarks>This method is intended for use with files containing environment variable
        /// assignments, such as .env files. Only variable names are extracted</remarks>
        /// <param name="fileLines">An array of strings representing the lines of the file to be scanned for environment variable definitions.
        /// Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings with the names
        /// of environment variables found in the provided file lines. The list will be empty if no environment
        /// variables are detected.</returns>
        Task<List<string>> ReadEnvVariables(string[] fileLines);

        /// <summary>
        /// Updates the environment file to reflect the differences between local and repository environment variables
        /// and their contents.
        /// </summary>
        /// <param name="localEnvVars">A list of environment variable names present in the local environment. Used to determine which variables
        /// should be updated or retained.</param>
        /// <param name="repoEnvVars">A list of environment variable names present in the repository environment. Used to identify variables that
        /// may need to be added or removed.</param>
        /// <param name="localEnvContent">An array of strings representing the contents of the local environment file. Each element corresponds to a
        /// line in the file.</param>
        /// <param name="repoEnvContent">An array of strings representing the contents of the repository environment file. Each element corresponds
        /// to a line in the file.</param>
        /// <returns>A task that represents the asynchronous update operation. The task result contains updated lines to write in the local env file</returns>
        Task<List<string>> UpdateEnvFile(List<string> localEnvVars, List<string> repoEnvVars, string[] localEnvContent, string[] repoEnvContent);

    }
}
