namespace Conesoft.Server_Host.Features.ActivePorts.Interfaces;

public interface IControlActivePorts
{
    Task FindPort(string name);
    Task RemovePort(string name);
}
