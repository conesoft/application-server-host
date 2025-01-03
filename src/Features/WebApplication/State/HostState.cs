namespace Conesoft.Server_Host.Features.WebApplication.State;

public record Host(ILookup<string, Host.Service> Services)
{
    public static Host Empty { get; } = new(Array.Empty<Service>().ToLookup(s => s.Category));

    public record Service(string Name, int Process, ushort? Port, string Category);
}