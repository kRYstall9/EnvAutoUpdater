using EnvAutoUpdater.src;
using EnvAutoUpdater.src.Models;
using EnvAutoUpdater.src.Services;
using EnvAutoUpdater.src.Services.Interfaces;
using EnvAutoUpdater.src.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

#region CONFIG LOADING
Config? config;

try
{
    config = await ConfigLoader.Load();
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading config: {ex.Message}");
    return;
}
#endregion

var logLevel = LogLevelHelper.Parse(config?.LogLevel);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddHttpClient<IEnvUpdaterService, EnvUpdaterService>();
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole(options =>
        {
            options.FormatterName = nameof(CustomLogFormatter);
        });

        logging.AddConsoleFormatter<CustomLogFormatter, SimpleConsoleFormatterOptions>(options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
        });

        logging.AddFilter("Microsoft", LogLevel.None);
        logging.AddFilter("System", LogLevel.None);
        logging.SetMinimumLevel(logLevel);

    })
    .Build();

await host.RunAsync();