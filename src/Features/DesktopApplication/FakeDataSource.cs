namespace Conesoft.Server_Host.Features.DesktopApplication;

public record FakeDataContext(FakeWebsite[] Websites, FakeService[] Services);
public record FakeService(string Name, string ProcessDescription, int ProcessId);
public record FakeWebsite(string Name, string ProcessDescription, int ProcessId, ushort Port);
public static class FakeDataSource
{
    public static FakeDataContext Context { get; private set; } = new(
        Websites: [
            new("davepermen.net", "davepermen.net.exe", ProcessId: 69420, Port: 54321),
            new("conesoft.net", "conesoft.net.exe", ProcessId: 42069, Port: 12345),
            ],
        Services: [
            new("Test Service", "Test Service.exe", ProcessId: 69),
            new("Second Service", "Second Service.exe", ProcessId: 420)
        ]
    );
}