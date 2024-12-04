using Conesoft.Files;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.HostEnvironment;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Serilog;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class HostDeploymentHandler(HostEnvironmentInfo environment, ActiveProcessesService activeProcesses, IHostApplicationLifetime app) :
    IListener<StartDeployment>,
    IListener<StopDeployment>
{
    void IListener<StartDeployment>.Listen(StartDeployment message)
    {
        Log.Information("Starting Host Deployment for {message}", message.Source.NameWithoutExtension);
        var target = environment.Global.Live / message.Source.Parent.Name / message.Source.NameWithoutExtension;


        if (message.Source.Info.LastWriteTimeUtc > target.Info.LastWriteTimeUtc)
        {
            Log.Information("New Version for Host found");
            try
            {
                message.Source.AsZip().ExtractTo(target);
            }
            catch (Exception e)
            {
                Log.Error("Could not extract to {directory} due to {exception}", target, e);
            }

            if (target.FilteredFiles("*.exe", allDirectories: false).FirstOrDefault() is File executable)
            {
                var start = new ProcessStartInfo(executable.Path)
                {
                    WorkingDirectory = target.Path,
                    Arguments = "-deploy-with-processes " + string.Join(" ", activeProcesses.Services.Values.Select(p => p.Id)),
                    CreateNoWindow = true
                };
                Process.Start(start);
                app.StopApplication();
            }
        }
        else
        {
            Log.Information("Detected Version is not new");
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        Log.Information("Ignoring Stop Message for {message}", message.Source.NameWithoutExtension);
    }
}