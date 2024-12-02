using Conesoft.Files;
using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.ActivePorts.Services;
using Conesoft.Server_Host.Features.ActiveProcesses.Messages;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Conesoft.Server_Host.Features.MediatorService.Services;
using Conesoft.Server_Host.Features.WebApplication.Messages;
using Conesoft.Server_Host.Helpers;
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
            Websites: activeProcesses.Services.Where(p => activePorts.Ports.ContainsKey(p.Key)).Select(p => new Host.Website(
                Name: p.Key,
                Description: File.From(p.Value.StartInfo.FileName).Name,
                Process: p.Value.Id,
                Port: activePorts.Ports[p.Key]
            )).ToArray(),
            Services: activeProcesses.Services.Where(p => activePorts.Ports.ContainsKey(p.Key) == false).Select(p => new Host.Service(
                Name: p.Key,
                Description: File.From(p.Value.StartInfo.FileName).Name,
                Process: p.Value.Id
            )).ToArray()
        );

        mediator.Notify(new HostStateChanged());
    }

    void IListener<OnPortFound>.Listen(OnPortFound message) => UpdateState();

    void IListener<OnProcessKilled>.Listen(OnProcessKilled message) => UpdateState();

    void IListener<OnNewProcessLaunched>.Listen(OnNewProcessLaunched message) => UpdateState();
}