using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.Deployments.Services;
using Conesoft.Server_Host.Features.MediatorService.Extensions;

namespace Conesoft.Server_Host.Features.Deployments.Extensions;

static class AddDeploymentWatcherExtensions
{
    public static WebApplicationBuilder AddDeploymentWatcher(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<DeploymentWatcher>();

        builder.Services
            .AddKeyedMediatingSingleton<ServiceDeploymentHandler>("Services")
            .AddKeyedMediatingSingleton<ServiceDeploymentHandler>("Websites")
            .AddKeyedMediatingSingleton<ServiceDeploymentHandler>("Plugins")
            .AddKeyedMediatingSingleton<HostDeploymentHandler>("Host")
            .AddKeyedMessage<StartDeployment>(m => m.Source.Parent.Name)
            .AddKeyedMessage<StopDeployment>(m => m.Source.Parent.Name)
            ;
        return builder;
    }
}