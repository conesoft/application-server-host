using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
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
            try
            {
                var start = new ProcessStartInfo(executable.Path, $"--urls=https://127.0.0.1:0/")
                {
                    WorkingDirectory = directory.Path,
                    CreateNoWindow = true
                };
                activeProcesses.Launch(name, category, start);
                activePorts.FindPort(name);
            }
            catch (Exception ex)
            {
                Log.Error("exception {exception}", ex);
            }
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        var name = message.Source.NameWithoutExtension;
        var category = message.Source.Parent.Name;

        Log.Information("Stopping {serviceType} Deployment for {message}", category, name);
        activePorts.RemovePort(name);
        activeProcesses.Kill(name);

        var directory = environment.Global.Live / category / name;
        directory.Delete();
    }
}