namespace Conesoft.Server_Host.Features.WebApplication.State;

public record Host(Host.Website[] Websites, Host.Service[] Services)
{
    public static Host Empty { get; } = new([], []);

    public record Website(string Name, int Process, ushort Port, string Description);
    public record Service(string Name, int Process, string Description);
}