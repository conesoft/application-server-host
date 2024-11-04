using Conesoft.Server_Host.Features.ActivePorts.Messages;
using Conesoft.Server_Host.Features.Mediator.Interfaces;
using ControlzEx.Theming;
using System.Windows;

namespace Conesoft.Server_Host.Features.DesktopApplication;

public class MainWindowMessageBroker :
    IHandler<Messages.SetMainWindowIcon>,
    IHandler<Messages.SetMainWindowState>,
    IListener<Messages.OnThemeChanged>,
    IListener<Messages.OnMainWindowStateChanged>,
    IListener<OnPortFound>,
    IListener<ActiveProcesses.Messages.OnProcessKilled>
{
    private MainWindow? _my = default;

    readonly Queue<Action<MainWindow>> messages = [];

    public MainWindow? My
    {
        get => _my; set
        {
            _my = value;
            foreach(var message in messages)
            {
                Dispatch(message);
            }
            messages.Clear();
        }
    }

    void Dispatch(Action<MainWindow> action)
    {
        if (My != null)
        {
            My.Dispatcher.Invoke(action, My);
        }
        else
        {
            messages.Enqueue(action);
        }
    }

    void IListener<Messages.OnMainWindowStateChanged>.Listen(Messages.OnMainWindowStateChanged message)
    {
        Dispatch(my =>
        {
            my.ShowInTaskbar = my.WindowState != WindowState.Minimized;
            my.WindowStyle = my.WindowState != WindowState.Minimized ? WindowStyle.None : WindowStyle.ToolWindow; // hides from alt-tab
        });
    }

    void IHandler<Messages.SetMainWindowIcon>.Handle(Messages.SetMainWindowIcon message)
    {
        Dispatch(my => my.Icon = message.Icon);
    }

    void IHandler<Messages.SetMainWindowState>.Handle(Messages.SetMainWindowState message)
    {
        Dispatch(my => my.WindowState = message.State);
    }

    void IListener<Messages.OnThemeChanged>.Listen(Messages.OnThemeChanged message)
    {
        Dispatch(my => ThemeManager.Current.SyncTheme(ThemeSyncMode.SyncWithAppMode));
    }

    void IListener<OnPortFound>.Listen(OnPortFound message)
    {
        Dispatch(my => my.RefreshDataContext());
    }

    void IListener<ActiveProcesses.Messages.OnProcessKilled>.Listen(ActiveProcesses.Messages.OnProcessKilled message)
    {
        Dispatch(my => my.RefreshDataContext());
    }
}
