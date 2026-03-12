using EnvAutoUpdater.src.Models;
using EnvAutoUpdater.src.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace EnvAutoUpdater.src.Services
{
    public class EnvUpdaterService(ILogger<IEnvUpdaterService> _logger, HttpClient _httpClient, IConfigService _configService) : IEnvUpdaterService
    {
        public async Task RunCheck(CancellationToken cancellationToken = default)
        {
            Config config = await _configService.GetConfig();

            if (config.ServicesToUpdate == null || config.ServicesToUpdate.Count == 0)
            {
                _logger.LogWarning("No services configured for .env updates. Skipping update process.");
                return;
            }

            foreach (var service in config.ServicesToUpdate)
            {
                _logger.LogInformation($"Starting .env update process for service: {service.ServiceName}");

                #region READ LOCAL AND REPO .ENV FILES
                string[] localEnvContent = await ReadLocalEnvFile(service.EnvLocalFilePath!, cancellationToken);
                if (localEnvContent.Length == 0)
                {
                    _logger.LogWarning($"Skipping update for service {service.ServiceName} due to issues reading the local .env file.");
                    continue;
                }

                string? updatedEnvContent = await ReadUpdatedEnvFileFromRepository(service.EnvRepoUrl!, cancellationToken);
                if (string.IsNullOrEmpty(updatedEnvContent))
                {
                    _logger.LogWarning($"Skipping update for service {service.ServiceName} due to issues fetching the .env file from the repository.");
                    continue;
                }

                #endregion

                string[] repoEnvLines = updatedEnvContent.Split('\n');

                #region READ ENV VARIABLES FROM LOCAL AND REPO .ENV FILES

                List<string> localEnvVars = await ReadEnvVariables(localEnvContent);
                List<string> repoEnvVars = await ReadEnvVariables(repoEnvLines);

                #endregion

                bool isAlreadyUpdated = await IsFileAlreadyUpdated(localEnvVars, repoEnvVars);
                if (isAlreadyUpdated)
                {
                    _logger.LogInformation($"The local .env file for service {service.ServiceName} is already up to date. No update needed.");
                    continue;
                }

                try
                {
                    _logger.LogInformation($"The local .env file for service {service.ServiceName} is outdated. Proceeding to update it with the latest changes");
                    var updatedContent = await UpdateEnvFile(localEnvVars, repoEnvVars, localEnvContent, repoEnvLines);

                    #region CREATE BACKUP FILE
                    //Create a backup env file before writing the updated content, in case something goes wrong during the write process. The backup file will be created in the same directory as the original file with the name format: .env.bak_TIMESTAMP
                    string backupFilePath = $"{service.EnvLocalFilePath}.bak_{DateTime.Now:yyyyMMddHHmmss}";
                    await File.WriteAllLinesAsync(backupFilePath, localEnvContent, cancellationToken);

                    #endregion

                    await File.WriteAllLinesAsync(service.EnvLocalFilePath!, updatedContent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to write the updated .env content to the local file for service {service.ServiceName}. Exception: {ex.Message}");
                }
            }
        }

        public Task<bool> IsFileAlreadyUpdated(List<string> localEnvVars, List<string> repoEnvVars)
        {
            bool alreadyUpdated = true;

            foreach (var repoVar in repoEnvVars)
            {
                if (!localEnvVars.Contains(repoVar))
                {
                    _logger.LogDebug($"The environment variable '{repoVar}' is NOT present in the local .env file.");
                    alreadyUpdated = false;
                    break;
                }
            }

            return Task.FromResult(alreadyUpdated);
        }

        public async Task<string[]> ReadLocalEnvFile(string envFilePath, CancellationToken cancellationToken)
        {
            string[] localEnvContent;

            try
            {
                if (!File.Exists(envFilePath))
                {
                    _logger.LogWarning($"The local .env file at {envFilePath} does not exist.");
                    return [];
                }

                localEnvContent = await File.ReadAllLinesAsync(path: envFilePath, cancellationToken: cancellationToken);
                return localEnvContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to read the local .env file at {envFilePath}. Exception: {ex.Message}");
                return [];
            }
        }

        public async Task<string?> ReadUpdatedEnvFileFromRepository(string repoUrl, CancellationToken cancellationToken)
        {
            if (!repoUrl.StartsWith("https://raw."))
            {
                if (repoUrl.Contains(".env"))
                {
                    // If the URL ends with .env but doesn't start with https://raw., we can attempt to convert it to a raw URL format
                    repoUrl = repoUrl.Replace("github.com", "raw.githubusercontent.com").Replace("/blob/", "/");
                }
                else
                {
                    _logger.LogError($"The provided repository URL {repoUrl} is not in the expected format. It should end with '.env'.");
                    return null;
                }
            }

            var response = await _httpClient.GetAsync(repoUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch the updated .env file from {repoUrl}. Status code: {response.StatusCode}");
                return null;
            }

            string? updatedEnvContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return updatedEnvContent;
        }

        public Task<List<string>> ReadEnvVariables(string[] fileLines)
        {
            List<string> envVars = [];

            foreach (string line in fileLines)
            {
                try
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(line.Trim()) || !line.TrimStart().Contains('='))
                    {
                        _logger.LogDebug($"Skipping line: '{line}' as it is either empty, a comment or an example value of a previous selected env variable");
                        continue;
                    }

                    string envVarName = line.Split('=')[0].Trim();

                    if (!string.IsNullOrEmpty(envVarName))
                    {
                        envVarName = envVarName.Trim().StartsWith('#') ? envVarName.Split('#')[1].Trim() : envVarName; // Handle the case where the variable is commented out by removing the '#' character

                        if (envVarName.Contains(' ')) // If the variable name contains spaces, it's likely that it's an example value of a selected env variable, so we skip it
                        {
                            _logger.LogDebug($"Skipping line: '{line}' as it is likely an example value of selected env variable due to the presence of spaces in the variable name");
                            continue;
                        }

                        envVars.Add(envVarName);
                        _logger.LogDebug($"Extracted environment variable: '{envVarName}' from line: '{line}'.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to parse line: '{line}'. Exception: {ex.Message}");
                }
            }

            return Task.FromResult(envVars.Where(x => !string.IsNullOrEmpty(x.Trim())).Distinct().ToList());
        }

        public Task<List<string>> UpdateEnvFile(List<string> localEnvVars, List<string> repoEnvVars, string[] localEnvContent, string[] repoEnvContent)
        {
            string marker = "#-------------- This section was automatically updated by EnvAutoUpdater -----------";
            var result = new List<string>(localEnvContent);

            if (!result.Contains(marker))
            {
                result.Add("\n\n#-------------- This section was automatically updated by EnvAutoUpdater -----------\n\n");
            }

            foreach (string repoVar in repoEnvVars)
            {
                try
                {
                    string repoLine = repoEnvContent.FirstOrDefault(line => line.StartsWith(repoVar)) ?? string.Empty;

                    if (string.IsNullOrEmpty(repoLine))
                    {
                        _logger.LogWarning($"The variable '{repoVar}' was expected to be found in the repository .env content but was not located. Skipping this variable.");
                        continue;
                    }

                    int repoLineIndex = Array.IndexOf(repoEnvContent, repoLine);

                    repoLine = "# " + repoLine; // Comment out the variable line from the repo to avoid issues

                    //Expect to find multi line variable values, like a json string, so we need to check for the end of the variable value by looking for the next line that starts with a new variable or is empty or a comment
                    if (repoLineIndex < repoEnvContent.Length - 1)
                    {
                        for (int i = repoLineIndex + 1; i < repoEnvContent.Length; i++)
                        {
                            if (string.IsNullOrEmpty(repoEnvContent[i].Trim()) || repoEnvContent[i].TrimStart().StartsWith('#') || repoEnvContent[i].Contains('='))
                            {
                                break;
                            }
                            repoLine += "\n# " + repoEnvContent[i];
                        }
                    }

                    List<string> commentsFromRepo = [];

                    //Add comments and empty lines before the variable from the repo, if they exist, to the list of lines to add to the local env file. We will add them right before the variable line, so we need to find them first
                    while (repoLineIndex > 0 && repoEnvContent[repoLineIndex - 1].TrimStart().StartsWith('#'))
                    {
                        commentsFromRepo.Insert(0, repoEnvContent[repoLineIndex - 1]);
                        repoLineIndex--;
                    }

                    bool isVarInLocalEnv = localEnvVars.Contains(repoVar);

                    if (!isVarInLocalEnv)
                    {
                        _logger.LogDebug($"Adding missing environment variable '{repoVar}' to the local .env file.");
                        result.AddRange(commentsFromRepo);
                        result.Add(repoLine + "\n");
                    }
                    else if (commentsFromRepo.Count > 0)
                    {
                        _logger.LogDebug($"The environment variable '{repoVar}' is already present in the local .env file. Proceeding to update the comments");

                        var regex = new Regex($@"^#+\s*{Regex.Escape(repoVar)}\s*=", RegexOptions.Singleline);
                        int localLineIndex = result.FindIndex(line => line.StartsWith(repoVar));

                        if(localLineIndex == -1)
                        {
                            localLineIndex = result.FindIndex(line => regex.IsMatch(line));
                        }

                        _logger.LogDebug($"The line index of the variable '{repoVar}' in the local .env file is: {localLineIndex}");
                        if (localLineIndex < 0) continue;

                        while (localLineIndex > 0 && result[localLineIndex - 1].TrimStart().StartsWith('#'))
                        {
                            string prevLine = result[localLineIndex - 1];
                            string prevLineTrimmed = prevLine.TrimStart();

                            bool isEmpty = string.IsNullOrEmpty(prevLineTrimmed);
                            bool isCommentedVar = localEnvVars.Any(v =>
                            {
                                _logger.LogDebug($"ENV VAR: {v} - PREVLINE: {prevLine} - ISMATCH: {Regex.IsMatch(prevLine, $@"^#\s*{Regex.Escape(v)}\s*=")}");
                                return Regex.IsMatch(prevLine, $@"^#\s*{Regex.Escape(v)}\s*=");
                            });

                            bool isPureComment = prevLineTrimmed.StartsWith('#') && !isCommentedVar;

                            if (!isPureComment && !isEmpty && !commentsFromRepo.Contains(prevLine, StringComparer.InvariantCultureIgnoreCase))
                            {
                                _logger.LogDebug($"Stopping the removal of lines before the variable '{repoVar}' at line index {localLineIndex - 1} because the line is not a pure comment or empty line. Line content: '{prevLine}'");
                                break;
                            }

                            result.RemoveAt(localLineIndex - 1);
                            localLineIndex--;
                        }

                        result.InsertRange(localLineIndex, commentsFromRepo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while trying to update the local .env file with the variable '{repoVar}'. Exception: {ex.Message}");
                }
            }
            return Task.FromResult(result);
        }
    }
}
