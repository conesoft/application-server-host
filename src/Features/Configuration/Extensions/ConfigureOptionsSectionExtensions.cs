using Microsoft.Extensions.Options;

namespace Conesoft.Server_Host.Features.Configuration.Extensions;

static class ConfigureOptionsSectionExtensions
{
    public static IServiceCollection ConfigureOptionsSection<T>(this IServiceCollection services, string section) where T : class
    {
        services.AddTransient<SectionName<T>>(_ => new(section));
        services.ConfigureOptions<Setup<T>>();
        return services;
    }

    class Setup<T>(IConfiguration configuration, SectionName<T> sectionName) : IConfigureOptions<T> where T : class
    {
        public void Configure(T options) => configuration.GetSection(sectionName.Section).Bind(options);

    }

    record SectionName<T>(string Section);
}