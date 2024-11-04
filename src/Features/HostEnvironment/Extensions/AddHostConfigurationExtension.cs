namespace Conesoft.Server_Host.Features.HostEnvironment.Extensions;

static class AddHostEnvironmentInfoExtension
{
    public static WebApplicationBuilder AddHostEnvironmentInfo(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<HostEnvironmentInfo>();
        return builder;
    }
}