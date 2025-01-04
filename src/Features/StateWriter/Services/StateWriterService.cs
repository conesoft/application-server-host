using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.ActivePorts.Services;
using Conesoft.Server_Host.Features.ActiveProcesses.Messages;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Microsoft.VisualStudio.Threading;
using Serilog;
using Host = Conesoft.Server_Host.Features.StateWriter.State.Host;

namespace Conesoft.Server_Host.Features.StateWriter.Services;

class StateWriterService(ActiveProcessesService activeProcesses, ActivePortsService activePorts, HostEnvironment environment) :
    BackgroundService,
    IListener<OnPortFound>,
    IListener<OnNewProcessLaunched>,
    IListener<OnProcessKilled>
{
    readonly AsyncAutoResetEvent e = new(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("starting {service}", "state writer");
        while (stoppingToken.IsCancellationRequested == false)
        {
            await e.WaitAsync(stoppingToken).ReturnWhenCancelled();

            var state = new Host(
                Services: activeProcesses.Services
                    .Select(s => new Host.Service(
                        Name: s.Key,
                        Process: s.Value.Process.Id,
                        Port: activePorts.Ports.TryGetValue(s.Key, out var value) ? value : null,
                        Category: s.Value.Category))
                    .ToArray()
            );

            var file = environment.Global.Storage / "host" / Filename.From("state", "json");
            file.Parent.Create();

            await file.WriteAsJson(state);

            Log.Information("{service}: state written", "state writer");
        }
        Log.Information("stopping {service}", "state writer");
    }

    void IListener<OnPortFound>.Listen(OnPortFound message) => e.Set();

    void IListener<OnProcessKilled>.Listen(OnProcessKilled message) => e.Set();

    void IListener<OnNewProcessLaunched>.Listen(OnNewProcessLaunched message) => e.Set();
}