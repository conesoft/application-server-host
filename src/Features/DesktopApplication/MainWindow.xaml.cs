using Conesoft.Files;
using Conesoft.Server_Host.Features.ActivePorts.Services;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.Mediator.Services;
using MahApps.Metro.Controls;

namespace Conesoft.Server_Host.Features.DesktopApplication;

public partial class MainWindow : MetroWindow
{
    private readonly MediatorService mediator;
    private readonly ActiveProcessesService activeProcesses;
    private readonly ActivePortsService activePorts;

    public MainWindow(MediatorService mediator, ActiveProcessesService activeProcesses, ActivePortsService activePorts, MainWindowMessageBroker broker)
    {
        this.mediator = mediator;
        this.activeProcesses = activeProcesses;
        this.activePorts = activePorts;
        InitializeComponent();
        DotNetVersion.Text = $"running on .NET {Environment.Version.Major}";
        broker.My = this;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        mediator.Notify(new Messages.OnMainWindowStateChanged(WindowState));
        base.OnStateChanged(e);
    }

    public void RefreshDataContext()
    {
        this.DataContext = new
        {
            Websites = activeProcesses.Services.Where(p => activePorts.Ports.ContainsKey(p.Key)).Select(p => new
            {
                Name = p.Key,
                ProcessDescription = File.From(p.Value.StartInfo.FileName).Name,
                ProcessId = p.Value.Id,
                Port = activePorts.Ports[p.Key]
            }).ToArray(),
            Services = activeProcesses.Services.Where(p => activePorts.Ports.ContainsKey(p.Key) == false).Select(p => new
            {
                Name = p.Key,
                ProcessDescription = File.From(p.Value.StartInfo.FileName).Name,
                ProcessId = p.Value.Id
            }).ToArray()
        };
    }
}
