using Conesoft.Server_Host.Features.MediatorService.Extensions;
using Conesoft.Server_Host.Features.StateWriter.Services;

namespace Conesoft.Server_Host.Features.StateWriter.Extensions;

static class AddStateWriterExtensions
{
    public static WebApplicationBuilder AddStateWriter(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediatingHostedService<StateWriterService>();
        return builder;
    }
}