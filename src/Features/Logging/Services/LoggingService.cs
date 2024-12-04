using Conesoft.Files;
using Conesoft.Server_Host.Features.HostEnvironmentInfo;
using Serilog;
using Serilog.Formatting.Compact;

namespace Conesoft.Server_Host.Features.Logging.Services;

class LoggingService : IHostedService
{
    private readonly HostEnvironment environment;

    public LoggingService(HostEnvironment environment)
    {
        this.environment = environment;
        if (environment.IsInHostedEnvironment)
        {
            var txt = environment.Root / "Logs" / "as Text" / Filename.From($"{environment.ApplicationName} - ", "txt");
            var log = environment.Root / "Logs" / Filename.From($"{environment.ApplicationName} - ", "log");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    txt.Path,
                    buffered: false,
                    shared: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: null,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                )
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    log.Path,
                    buffered: false,
                    shared: true,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: null,
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                ).CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Unhandled Exception");
                }
                else
                {
                    Log.Fatal("Unhandled Exception: {ex}", e.ExceptionObject);
                }
            };
        }
        else
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information($"app '{environment.ApplicationName}' started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information($"app '{environment.ApplicationName}' stopped");
        return Task.CompletedTask;
    }
}