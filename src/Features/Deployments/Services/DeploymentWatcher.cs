using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.MediatorService.Services;
using Serilog;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class DeploymentWatcher(HostEnvironment info, Mediator mediator) : IHostedService
{
    CancellationTokenSource? cancellation;

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        var source = info.Global.Deployments;
        var target = info.Global.Live;
        source.Create();
        target.Create();

        //Log.Information("Clean all deployments"); // needs fixing as the app is deployed in this. more granular cleaning needed
        //target.Delete();
        //target.Create();

        Log.Information("DeploymentWatcher starting to watch {source}", source);

        cancellation = source.Changes(async changes =>
        {
            Log.Information("Old {entries}", changes.Deleted.Concat(changes.Changed).Select(f => f.Name));
            Log.Information("New {entries}", changes.Added.Concat(changes.Changed).Select(f => f.Name));

            foreach (var file in changes.Deleted.Concat(changes.Changed).Files().Where(f => f.Parent.Parent == source))
            {
                if (file.Exists)
                {
                    await file.WaitTillReadyAsync();
                }
                Log.Information("Removing deployment of {file} in {type}", file.NameWithoutExtension, file.Parent.Name);
                mediator.Notify(new StopDeployment(Source: file));
                Log.Information("Removed deployment of {file} in {type}", file.NameWithoutExtension, file.Parent.Name);
            }
            foreach (var file in changes.Added.Concat(changes.Changed).Files().Where(f => f.Parent.Parent == source))
            {
                if (file.Exists)
                {
                    await file.WaitTillReadyAsync();
                }
                Log.Information("Deploying {file} to {type}", file.NameWithoutExtension, file.Parent.Name);
                mediator.Notify(new StartDeployment(Source: file));
                Log.Information("Deployed {file} to {type}", file.NameWithoutExtension, file.Parent.Name);
            }
        }, all: true);

        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => cancellation?.CancelAsync() ?? Task.CompletedTask;
}
