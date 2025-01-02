using Conesoft.Server_Host.Features.ActiveProcesses.Helpers;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Messages;
using Conesoft.Server_Host.Features.MediatorService.Services;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Services;

public class ActiveProcessesService(Mediator mediator) : IControlActiveProcesses
{
    readonly Dictionary<string, Process> services = [];
    readonly ProcessTracker tracker = new();

    public IReadOnlyDictionary<string, Process> Services => services;

    void IControlActiveProcesses.Kill(string name)
    {
        if (services.TryGetValue(name, out var process))
        {
            mediator.Notify(new OnProcessGettingKilled(name, process));
            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch (Exception)
            {
            }
            services.Remove(name);
            mediator.Notify(new OnProcessKilled(name));
        }
    }

    void IControlActiveProcesses.Launch(string name, ProcessStartInfo startInfo)
    {
        (this as IControlActiveProcesses).Kill(name);
        mediator.Notify(new OnNewProcessGettingLaunched(name));
        if (tracker.Track(Process.Start(startInfo)) is Process process)
        {
            services[name] = process;
            process.EnableRaisingEvents = true;
            process.Exited += (_,_) =>
            {
                (this as IControlActiveProcesses).Kill(name);
            };
            mediator.Notify(new OnNewProcessLaunched(name, process));
        }
    }
}
