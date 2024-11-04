using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActivePorts.Services;

namespace Conesoft.Server_Host.Features.ActivePorts.Extensions;

static class AddActivePortsExtensions
{
    public static WebApplicationBuilder AddActivePorts(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ActivePortsService>();
        builder.Services.AddTransient<IControlActivePorts>(s => s.GetRequiredService<ActivePortsService>());
        return builder;
    }
}