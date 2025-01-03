using Conesoft.Network_Connections;
using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.MediatorService.Services;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActivePorts.Services;

public class ActivePortsService(Mediator mediator, ActiveProcessesService processes, IHttpClientFactory clients) : IControlActivePorts
{
    readonly HttpClient client = clients.CreateClient();

    readonly Dictionary<string, ushort> ports = [];

    public IReadOnlyDictionary<string, ushort> Ports => ports;

    async void IControlActivePorts.FindPort(string name)
    {
        if (IsValidDomain(name) && processes.Services.TryGetValue(name, out var entry))
        {
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (await FindHttpsPortOnProcess(entry.Process, timeout.Token).NullIfCancelled() is Connection connection)
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

    Task<bool> IsHttpsPort(ushort port, CancellationToken ct) => client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://localhost:{port}/"), ct).ContinueWith(t => t.IsCompletedSuccessfully);

    async Task<Connection?> FindHttpsPortOnProcess(Process process, CancellationToken ct = default)
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

    static bool IsValidDomain(string domain) => Uri.TryCreate($"https://{domain}", UriKind.Absolute, out var result) && result.Scheme == Uri.UriSchemeHttps;
}
