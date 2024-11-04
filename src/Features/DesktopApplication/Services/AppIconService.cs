using Conesoft.Server_Host.Features.Mediator.Interfaces;
using Conesoft.Server_Host.Features.Mediator.Services;
using System.Windows.Media.Imaging;

namespace Conesoft.Server_Host.Features.DesktopApplication.Services;

public class AppIconService(MediatorService mediator) : IListener<Messages.OnThemeChanged>
{
    readonly Dictionary<Theme, BitmapImage> themedIcons = new()
    {
        [Theme.Light] = new(new(@"Icons\Server.Dark.ico", UriKind.Relative)),
        [Theme.Dark] = new(new(@"Icons\Server.Light.ico", UriKind.Relative))
    };

    void IListener<Messages.OnThemeChanged>.Listen(Messages.OnThemeChanged message)
    {
        mediator.Send(new Messages.SetMainWindowIcon(themedIcons[message.Theme]));
    }
}
