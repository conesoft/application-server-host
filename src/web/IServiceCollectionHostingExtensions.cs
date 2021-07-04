using Microsoft.Extensions.DependencyInjection;

namespace Conesoft.Host.Web
{
    public static class IServiceCollectionHostingExtensions
    {
        public static IServiceCollection AddHosting(this IServiceCollection services)
        {
            services.AddHttpForwarder();
            services.AddSingleton<Hosting>();

            return services;
        }
    }
}
