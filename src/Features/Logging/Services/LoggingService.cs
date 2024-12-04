using Conesoft.Files;
using Conesoft.Server_Host.Features.HostEnvironment;
using Serilog;
using Serilog.Formatting.Compact;

namespace Conesoft.Server_Host.Features.Logging.Services;

class LoggingService : IHostedService
{
    private readonly HostEnvironmentInfo environment;

    public LoggingService(HostEnvironmentInfo environment)
    {
        this.environment = environment;
        if (environment.Environment.IsInHostedEnvironment)
        {
            // TODO: Improve paths :D
            var path = environment.Global.Storage.Parent / "Logs" / $"{environment.Environment.Type} - {environment.Environment.Name}";

            var txt = path / "as Text" / Filename.From(environment.Environment.Name + " - ", "txt");
            var log = path / Filename.From(environment.Environment.Name + " - ", "log");

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
        Log.Information($"app '{environment.Environment.Name}' started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information($"app '{environment.Environment.Name}' stopped");
        return Task.CompletedTask;
    }
}