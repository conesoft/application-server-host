﻿using Conesoft.Server_Host.Features.ActiveProcesses.Helpers;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Messages;
using Conesoft.Server_Host.Features.Mediator.Services;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Services;

public class ActiveProcessesService(MediatorService mediator) : IControlActiveProcesses
{
    readonly Dictionary<string, Process> services = [];

    public IReadOnlyDictionary<string, Process> Services => services;

    void IControlActiveProcesses.Kill(string name)
    {
        if(services.TryGetValue(name, out var process))
        {
            mediator.Notify(new OnProcessGettingKilled(name, process));
            process.Kill();
            process.WaitForExit();
            services.Remove(name);
            mediator.Notify(new OnProcessKilled(name));
        }
    }

    void IControlActiveProcesses.Launch(string name, ProcessStartInfo startInfo)
    {
        (this as IControlActiveProcesses).Kill(name);
        mediator.Notify(new OnNewProcessGettingLaunched(name));
        if(ChildProcessTracker.Track(Process.Start(startInfo)) is Process process)
        {
            services[name] = process;
            mediator.Notify(new OnNewProcessLaunched(name, process));
        }
    }
}
