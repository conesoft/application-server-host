using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.Mediator.Interfaces;

namespace Conesoft.Server_Host.Features.Deployments.Services;

#pragma warning disable CS9113 // Der Parameter ist ungelesen.
class HostDeploymentHandler(IControlActiveProcesses activeProcesses, IControlActivePorts activePorts) :
#pragma warning restore CS9113 // Der Parameter ist ungelesen.
    IHandler<StartDeployment>,
    IHandler<StopDeployment>
{
    void IHandler<StartDeployment>.Handle(StartDeployment message)
    {
    }

    void IHandler<StopDeployment>.Handle(StopDeployment message)
    {
    }
}