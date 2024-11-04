using Conesoft.Server_Host.Features.Mediator.Interfaces;
using Conesoft.Server_Host.Features.Mediator.Services;
using System.Windows;

namespace Conesoft.Server_Host.Features.DesktopApplication.Services;

public class TrayIconService : IListener<Messages.OnMainWindowStateChanged>, IListener<Messages.OnThemeChanged>
{
    readonly NotifyIcon icon;

    readonly Dictionary<Theme, Icon> themedIcons = new()
    {
        [Theme.Light] = new("Icons/Server.Dark.ico"),
        [Theme.Dark] = new("Icons/Server.Light.ico")
    };

    public TrayIconService(MediatorService mediator)
    {
        icon = new();
        icon.Click += (sender, e) => mediator.Send(new Messages.SetMainWindowState(WindowState.Normal));
    }

    void IListener<Messages.OnMainWindowStateChanged>.Listen(Messages.OnMainWindowStateChanged message)
    {
        icon.Visible = message.State == WindowState.Minimized;
    }

    void IListener<Messages.OnThemeChanged>.Listen(Messages.OnThemeChanged message)
    {
        icon.Icon = themedIcons[message.Theme];
    }
}
