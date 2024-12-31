using Conesoft.Server_Host.Features.TrayIcon.Services;

namespace Conesoft.Server_Host.Features.TrayIcon.Extensions;

static class AddTrayIconExtensions
{
    public static WebApplicationBuilder AddTrayIcon(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<TrayIconService>();
        return builder;
    }

    public static IApplicationBuilder MapTemplate(this IApplicationBuilder app)
    {
        return app;
    }
}