using EnvAutoUpdater.src;
using EnvAutoUpdater.src.Services;
using EnvAutoUpdater.src.Services.Interfaces;
using EnvAutoUpdater.src.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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

        logging.SetMinimumLevel(LogLevel.Trace);
        logging.AddFilter<ConsoleLoggerProvider>((category, level) =>
        {
            if (category?.StartsWith("Microsoft") == true || category?.StartsWith("System") == true)
                return false;

            var currentConfig = ConfigLoader.LoadCached();
            var minLevel = LogLevelHelper.Parse(currentConfig?.LogLevel);
            return level >= minLevel;
        });

    })
    .Build();

await host.RunAsync();