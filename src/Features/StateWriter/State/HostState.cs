namespace Conesoft.Server_Host.Features.StateWriter.State;

public record Host(Host.Service[] Services)
{
    public static Host Empty { get; } = new([]);

    public record Service(string Name, int Process, ushort? Port, string Category);
}