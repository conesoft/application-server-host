using Conesoft.Server_Host.Features.MediatorService.Extensions;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Conesoft.Server_Host.Features.WebApplication.Messages;
using Conesoft.Server_Host.Features.WebApplication.Services;
using AspNet = Microsoft.AspNetCore.Builder;

namespace Conesoft.Server_Host.Features.WebApplication.Extensions;

static class AddWebApplicationExtensions
{
    public static WebApplicationBuilder AddWebApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediatingSingleton<WebApplicationService>();
        builder.Services.AddRazorPages();
        return builder;
    }

    public static AspNet.WebApplication MapWebApplication(this AspNet.WebApplication app)
    {
        app.MapStaticAssets();
        app.MapRazorPages();
        app.MapGet("/statechange", (IWaitOneMessage mediator) => mediator.WaitForNextMessage<HostStateChanged>());
        return app;
    }
}