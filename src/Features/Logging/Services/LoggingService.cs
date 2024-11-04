using Conesoft.Files;
using Conesoft.Server_Host.Features.HostEnvironment;
using Serilog;
using Serilog.Formatting.Compact;

namespace Conesoft.Server_Host.Features.Logging.Services;

class LoggingService : IHostedService
{
    private readonly HostEnvironmentInfo hosting;

    public LoggingService(HostEnvironmentInfo hosting)
    {
        this.hosting = hosting;
        if (hosting.IsInHostedEnvironment)
        {
            var path = hosting.Root / "Logs" / $"{hosting.Type} - {hosting.Name}";

            var txt = path / "as Text" / Filename.From(hosting.Name + " - ", "txt");
            var log = path / Filename.From(hosting.Name + " - ", "log");

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
        Log.Information($"app '{hosting.Name}' started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information($"app '{hosting.Name}' stopped");
        return Task.CompletedTask;
    }
}