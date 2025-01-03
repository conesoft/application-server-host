using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.ActivePorts.Services;
using Conesoft.Server_Host.Features.ActiveProcesses.Messages;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Conesoft.Server_Host.Features.MediatorService.Services;
using Conesoft.Server_Host.Features.WebApplication.Messages;
using Host = Conesoft.Server_Host.Features.WebApplication.State.Host;

namespace Conesoft.Server_Host.Features.WebApplication.Services;

class WebApplicationService(ActiveProcessesService activeProcesses, ActivePortsService activePorts, Mediator mediator) :
    IListener<OnPortFound>,
    IListener<OnNewProcessLaunched>,
    IListener<OnProcessKilled>
{
    readonly ChangeBroadcaster hostChanged = new();
    public Host Host { get; private set; } = Host.Empty;
    public Task WaitForStateChange() => hostChanged.WaitForChange();

    void UpdateState()
    {
        Host = new Host(
            Services: activeProcesses.Services
                .Select(s => new Host.Service(
                    Name: s.Key,
                    Process: s.Value.Process.Id,
                    Port: activePorts.Ports.TryGetValue(s.Key, out var value) ? value : null,
                    Category: s.Value.Category))
                .ToLookup(s => s.Category)
        );

        mediator.Notify(new HostStateChanged());
    }

    void IListener<OnPortFound>.Listen(OnPortFound message) => UpdateState();

    void IListener<OnProcessKilled>.Listen(OnProcessKilled message) => UpdateState();

    void IListener<OnNewProcessLaunched>.Listen(OnNewProcessLaunched message) => UpdateState();
}