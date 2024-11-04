using Conesoft.Hosting;

namespace Conesoft.Server_Host.Features.GarbageCollection.Extensions;

static class AddGarbageCollectionExtensions
{
    public static WebApplicationBuilder AddGarbageCollection(this WebApplicationBuilder builder)
    {
        builder.Services.AddPeriodicGarbageCollection(TimeSpan.FromMinutes(5));
        return builder;
    }
}