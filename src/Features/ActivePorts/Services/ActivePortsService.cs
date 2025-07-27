using Conesoft.Network_Connections;
using Conesoft.Server_Host.Features.ActivePorts.Interfaces;
using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.MediatorService.Services;
using Serilog;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActivePorts.Services;

public class ActivePortsService(Mediator mediator, ActiveProcessesService processes, IHttpClientFactory clients) : IControlActivePorts
{
    HttpClient Client => clients.CreateClient("shorttimeout");

    readonly Dictionary<string, ushort> ports = [];

    public IReadOnlyDictionary<string, ushort> Ports => ports;

    async Task IControlActivePorts.FindPort(string name)
    {
        if (IsValidDomain(name) && processes.Services.TryGetValue(name, out var entry))
        {
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            if (await FindHttpsPortOnProcess(entry.Process, timeout.Token).NullIfCancelled() is Connection connection)
            {
                lock (ports)
                {
                    ports[name] = connection.Local.Port;
                }
                mediator.Notify(new OnPortFound(name, connection.Local.Port));
            }
        }
    }

    Task IControlActivePorts.RemovePort(string name)
    {
        lock (ports)
        {
            ports.Remove(name);
        }
        return Task.CompletedTask;
    }

    Task<bool> IsHttpsPort(ushort port, CancellationToken ct) => Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://localhost:{port}/"), ct).ContinueWith(t => t.IsCompletedSuccessfully, TaskScheduler.Default);

    async Task<Connection?> FindHttpsPortOnProcess(Process process, CancellationToken ct = default)
    {
        while (ct.IsCancellationRequested == false)
        {
            try
            {
                var connections = process.GetListeningPorts();
                var connection = await connections.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(async c => await IsHttpsPort(c.Local.Port, ct), ct);
                if (connection != null)
                {
                    return connection;
                }
                await Task.Delay(200, ct);
            }
            catch (Exception ex)
            {
                Log.Error("exception {exception}", ex);
            }
        }
        return null;
    }

    static bool IsValidDomain(string domain) => Uri.TryCreate($"https://{domain}", UriKind.Absolute, out var result) && result.Scheme == Uri.UriSchemeHttps;
}
