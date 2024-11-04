using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Extensions;

static class AddActiveProcessesExtensions
{
    public static WebApplicationBuilder AddActiveProcesses(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ActiveProcessesService>();
        builder.Services.AddTransient<IControlActiveProcesses>(s => s.GetRequiredService<ActiveProcessesService>());
        return builder;
    }
}