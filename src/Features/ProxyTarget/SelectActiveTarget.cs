using Conesoft.Server_Host.Features.ActivePorts.Services;
using Conesoft.Server_Host.Features.ProxyTarget.Interfaces;

namespace Conesoft.Server_Host.Features.ProxyTarget;

class SelectActiveTarget(ActivePortsService activePorts) : ISelectProxyTarget
{
    public string? TargetFor(string domain) => activePorts.Ports.TryGetValue(domain.Replace(".localhost", ""), out var port) ? $"https://localhost:{port}" : null;
}