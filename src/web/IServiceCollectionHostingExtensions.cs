using Microsoft.Extensions.DependencyInjection;

namespace Conesoft.Server_Host.Web;

public static class IServiceCollectionHostingExtensions
{
    public static IServiceCollection AddHosting(this IServiceCollection services)
    {
        services.AddHttpForwarder();
        services.AddSingleton<Hosting>();

        return services;
    }
}