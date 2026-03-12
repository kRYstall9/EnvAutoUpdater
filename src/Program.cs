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
        services.AddSingleton<IConfigService, ConfigService>();
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

        var logLevelStr = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "INFORMATION";
        var logLevel = Enum.TryParse<LogLevel>(logLevelStr, true, out var parsedLogLevel) ? parsedLogLevel : LogLevel.Information;

        logging.SetMinimumLevel(logLevel);

    })
    .Build();

await host.RunAsync();