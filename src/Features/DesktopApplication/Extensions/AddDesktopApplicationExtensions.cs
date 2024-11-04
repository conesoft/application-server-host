using Conesoft.Server_Host.Features.DesktopApplication.Services;
using Conesoft.Server_Host.Features.Mediator.Extensions;

namespace Conesoft.Server_Host.Features.DesktopApplication.Extensions;

static class AddDesktopApplicationExtensions
{
    public static WebApplicationBuilder AddDesktopApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<MainApplication>();
        builder.Services.AddSingleton<MainWindow>();

        builder.Services.AddMediatingSingleton<MainWindowMessageBroker>();
        builder.Services.AddMediatingSingleton<TrayIconService>();
        builder.Services.AddMediatingSingleton<AppIconService>();

        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddHostedService<DesktopApplicationService>();

        return builder;
    }
}