using Conesoft.Server_Host.Features.Mediator.Services;
using Windows.UI.ViewManagement;

namespace Conesoft.Server_Host.Features.DesktopApplication.Services;

// based on https://www.meziantou.net/detecting-dark-and-light-themes-in-a-wpf-application.htm
public class ThemeService
{
    private readonly MediatorService mediator;
    private Theme current;
    readonly UISettings settings;

    public ThemeService(MediatorService mediator)
    {
        this.mediator = mediator;
        settings = new();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;
        Settings_ColorValuesChanged(default!, default!);
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        var next = IsColorLight(settings.GetColorValue(UIColorType.Background)) ? Theme.Light : Theme.Dark;
        if (current != next)
        {
            current = next;
            mediator.Notify(new Messages.OnThemeChanged(Theme: current));
        }
    }

    // From https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes?WT.mc_id=DT-MVP-5003978#know-when-dark-mode-is-enabled
    static bool IsColorLight(Windows.UI.Color clr) => ((5 * clr.G) + (2 * clr.R) + clr.B) > (8 * 128);
}
