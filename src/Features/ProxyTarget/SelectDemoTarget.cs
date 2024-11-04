using Conesoft.Server_Host.Features.ActivePorts.Services;
using Conesoft.Server_Host.Features.ProxyTarget.Interfaces;

namespace Conesoft.Server_Host.Features.ProxyTarget;

class SelectDemoTarget(ActivePortsService activePorts) : ISelectProxyTarget
{
    public string? TargetFor(string domain)
    {
        var port = activePorts.Ports.GetValueOrDefault(domain.Replace(".localhost", ""), (ushort)0);
        return  port > 0 ? $"https://localhost:{port}" : null;
    }
}