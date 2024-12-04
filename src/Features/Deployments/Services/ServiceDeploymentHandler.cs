using Conesoft.Files;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Serilog;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class ServiceDeploymentHandler(HostEnvironmentInfo.HostEnvironment environment, IControlActiveProcesses activeProcesses) :
    IListener<StartDeployment>,
    IListener<StopDeployment>
{
    void IListener<StartDeployment>.Listen(StartDeployment message)
    {
        Log.Information("Starting Service Deployment for {message}", message.Source.NameWithoutExtension);
        var target = environment.Global.Live / message.Source.Parent.Name / message.Source.NameWithoutExtension;
        message.Source.AsZip().ExtractTo(target);

        if (target.FilteredFiles("*.exe", allDirectories: false).FirstOrDefault() is File executable)
        {
            var start = new ProcessStartInfo(executable.Path)
            {
                WorkingDirectory = target.Path,
                CreateNoWindow = true
            };
            activeProcesses.Launch(message.Source.NameWithoutExtension, start);
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        Log.Information("Stopping Service Deployment for {message}", message.Source.NameWithoutExtension);
        activeProcesses.Kill(message.Source.NameWithoutExtension);

        var target = environment.Global.Live;
        var directory = target / message.Source.Parent.Name / message.Source.NameWithoutExtension;
        directory.Delete();
    }
}