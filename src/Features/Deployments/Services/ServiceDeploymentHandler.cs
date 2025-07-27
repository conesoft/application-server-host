using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Conesoft.Tools;
using Serilog;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class ServiceDeploymentHandler(HostEnvironment environment, IControlActiveProcesses activeProcesses, IControlActivePorts activePorts) :
    IListener<StartDeployment>,
    IListener<StopDeployment>
{
    void IListener<StartDeployment>.Listen(StartDeployment message)
    {
        var name = message.Source.NameWithoutExtension;
        var category = message.Source.Parent.Name;

        Log.Information("Starting {serviceType} Deployment for {message}", category, name);
        var target = environment.Global.Live;

        var directory = target / category / name;
        message.Source.AsZip().ExtractTo(directory);

        if (directory.FilteredFiles("*.exe", allDirectories: false).FirstOrDefault() is Files.File executable)
        {
            Safe.Try(() =>
            {
                var start = new ProcessStartInfo(executable.Path, $"--urls=https://127.0.0.1:0/")
                {
                    WorkingDirectory = directory.Path,
                    CreateNoWindow = true
                };
                activeProcesses.Launch(name, category, start);
                _ = activePorts.FindPort(name);
            });
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        var name = message.Source.NameWithoutExtension;
        var category = message.Source.Parent.Name;

        Log.Information("Stopping {serviceType} Deployment for {message}", category, name);
        _ = activePorts.RemovePort(name);
        activeProcesses.Kill(name);

        var directory = environment.Global.Live / category / name;
        _ = directory.Delete();
    }
}