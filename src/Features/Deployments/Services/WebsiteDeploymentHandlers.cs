using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Serilog;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class WebsiteDeploymentHandler(HostEnvironment environment, IControlActiveProcesses activeProcesses, IControlActivePorts activePorts) :
    IListener<StartDeployment>,
    IListener<StopDeployment>
{
    void IListener<StartDeployment>.Listen(StartDeployment message)
    {
        Log.Information("Starting Website Deployment for {message}", message.Source.NameWithoutExtension);
        var target = environment.Global.Live;

        var directory = target / message.Source.Parent.Name / message.Source.NameWithoutExtension;
        message.Source.AsZip().ExtractTo(directory);

        if (directory.FilteredFiles("*.exe", allDirectories: false).FirstOrDefault() is Files.File executable)
        {
            var start = new ProcessStartInfo(executable.Path, $"--urls=https://127.0.0.1:0/")
            {
                WorkingDirectory = directory.Path,
                CreateNoWindow = true
            };
            activeProcesses.Launch(message.Source.NameWithoutExtension, start);
            activePorts.FindPort(message.Source.NameWithoutExtension);
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        Log.Information("Stopping Website Deployment for {message}", message.Source.NameWithoutExtension);
        activePorts.RemovePort(message.Source.NameWithoutExtension);
        activeProcesses.Kill(message.Source.NameWithoutExtension);

        var directory = environment.Global.Live / message.Source.Parent.Name / message.Source.NameWithoutExtension;
        directory.Delete();
    }
}