using Conesoft.Network_Connections;
using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.Mediator.Services;
using System.Diagnostics;
using System.Net.Http;

namespace Conesoft.Server_Host.Features.ActivePorts.Services;

public class ActivePortsService(MediatorService mediator, ActiveProcessesService processes) : IControlActivePorts
{
    readonly Dictionary<string, ushort> ports = [];

    public IReadOnlyDictionary<string, ushort> Ports => ports;

    async void IControlActivePorts.FindPort(string name)
    {
        if(processes.Services.TryGetValue(name, out var process))
        {
            if (await FindHttpsPortOnProcess(process) is Connection connection)
            {
                ports[name] = connection.Local.Port;
                mediator.Notify(new OnPortFound(name, connection.Local.Port));
            }
        }
    }

    void IControlActivePorts.RemovePort(string name)
    {
        ports.Remove(name);
    }


    static Task<bool> IsHttpsPort(ushort port, CancellationToken ct) => new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://localhost:{port}/"), ct).ContinueWith(t => t.IsCompletedSuccessfully);

    static async Task<Connection?> FindHttpsPortOnProcess(Process process, CancellationToken ct = default)
    {
        while (ct.IsCancellationRequested == false)
        {
            var connections = process.GetListeningPorts();
            var connection = await connections.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(async c => await IsHttpsPort(c.Local.Port, ct), ct);
            if (connection != null)
            {
                return connection;
            }
            await Task.Delay(200, ct);
        }
        return null;
    }
}
