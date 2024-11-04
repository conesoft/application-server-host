namespace Conesoft.Server_Host.Features.ActivePorts.Interfaces;

public interface IControlActivePorts
{
    void FindPort(string name);
    void RemovePort(string name);
}
