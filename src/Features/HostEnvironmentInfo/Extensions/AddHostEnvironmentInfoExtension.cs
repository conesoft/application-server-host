namespace Conesoft.Server_Host.Features.HostEnvironmentInfo.Extensions;

static class AddHostEnvironmentInfoExtension
{
    public static WebApplicationBuilder AddHostEnvironmentInfo(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<HostEnvironment>();
        return builder;
    }
}