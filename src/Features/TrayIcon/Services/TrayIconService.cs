using Conesoft.Hosting;
using Microsoft.Win32;
using Serilog;
using SimpleTrayIcon;
using System.Management;
using System.Security.Principal;

namespace Conesoft.Server_Host.Features.TrayIcon.Services;

#pragma warning disable IDE0079
#pragma warning disable CA1416

partial class TrayIconService(IHostApplicationLifetime application, HostEnvironment environment) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {

        return Task.Run(() =>
        {
            Log.Information("starting service {0}", "tray icon");

            try
            {
                var exitMenuItem = new TrayMenuItem { Content = $"Exit Application" };
                exitMenuItem.Click += (s, e) => application.StopApplication();

                using var menu = new TrayMenu(new(CurrentIcon), environment.ApplicationName, true);
                menu.Items.Add(exitMenuItem);

                WatchThemeChange(() =>
                {
                    Log.Information("service {0}: changing icon to {1} theme", "tray icon", CurrentTheme == Theme.Dark ? "dark" : "light");
                    menu.Icon = new(CurrentIcon);
                });

                Log.Information("service {0} started, win32 message loop starting", "tray icon");

                NativeMethodsForTrayIconService.RunMessageLoop(stoppingToken);
            }
            catch (Exception)
            {
                Log.Information("tray icon service could not start, wrong platform?");
            }
            Log.Information("stopping service {0}", "tray icon");
        }, stoppingToken);
    }

    private static string CurrentIcon => $"Icons\\Server.{(CurrentTheme == Theme.Dark ? "Light" : "Dark")}.ico";

    private static void WatchThemeChange(Action onThemeChange)
    {
        var currentUser = WindowsIdentity.GetCurrent();
        var keyPath = "\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
        var valueName = "AppsUseLightTheme";
        var query = new WqlEventQuery(string.Format("SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_USERS' AND KeyPath='{0}\\\\{1}' AND ValueName='{2}'", currentUser.User!.Value, keyPath.Replace("\\", "\\\\"), valueName));
        var _watcher = new ManagementEventWatcher(query);
        _watcher.EventArrived += (_, _) => onThemeChange();
        _watcher.Start();
    }

    private static Theme CurrentTheme
    {
        get
        {
            var key = Registry.CurrentUser.OpenSubKey("""Software\Microsoft\Windows\CurrentVersion\Themes\Personalize""");
            var value = (int)(key?.GetValue("AppsUseLightTheme") ?? 0);
            return (Theme)value;
        }
    }

    enum Theme
    {
        Dark,
        Light
    }
}

#pragma warning restore CA1416
#pragma warning restore IDE0079