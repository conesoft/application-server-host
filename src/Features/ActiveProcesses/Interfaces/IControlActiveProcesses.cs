using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Interfaces;

public interface IControlActiveProcesses
{
    void Launch(string name, string category, ProcessStartInfo startInfo);
    void Kill(string name);
}
