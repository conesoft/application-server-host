using Conesoft.Files;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.HostEnvironment;
using Conesoft.Server_Host.Features.Mediator.Services;
using Serilog;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class DeploymentWatcher(HostEnvironmentInfo info, MediatorService mediator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var source = info.Root / "Deployments";
        var target = info.Root / "Live";
        source.Create();
        target.Create();

        Log.Information("Clean all deployments");
        target.Delete();
        target.Create();

        Log.Information("DeploymentWatcher starting to watch {source}", source);

        await foreach (var changes in source.Changes(allDirectories: true, cancellation: stoppingToken))
        {
            foreach (var file in changes.Deleted.Concat(changes.Changed).Files().Where(f => f.Parent.Parent == source))
            {
                Log.Information("Removing deployment of {file} in {type}", file.NameWithoutExtension, file.Parent.Name);
                mediator.Send(new StopDeployment(Source: file));
            }
            foreach (var file in changes.Added.Concat(changes.Changed).Files().Where(f => f.Parent.Parent == source))
            {
                Log.Information("Deploying {file} to {type}", file.NameWithoutExtension, file.Parent.Name);
                mediator.Send(new StartDeployment(Source: file));
            }
        }
    }
}
