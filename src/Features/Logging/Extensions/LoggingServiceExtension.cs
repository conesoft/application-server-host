using Conesoft.Server_Host.Features.Logging.Services;
using Serilog;

namespace Conesoft.Server_Host.Features.Logging.Extensions;

public static partial class LoggingServiceExtension
{
    public static WebApplicationBuilder AddLoggingService(this WebApplicationBuilder builder)
    {
        AttachConsole(-1);
        builder.Services.AddSingleton<LoggingService>();
        builder.Services.AddHostedService(s => s.GetRequiredService<LoggingService>()).AddSerilog();
        return builder;
    }

    public static IApplicationBuilder UseLoggingServiceOnRequests(this IApplicationBuilder app)
    {
        app.ApplicationServices.GetRequiredService<LoggingService>();
        app.UseSerilogRequestLogging();
        return app;
    }

    [System.Runtime.InteropServices.LibraryImport("kernel32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    static private partial bool AttachConsole(int dwProcessId);
}