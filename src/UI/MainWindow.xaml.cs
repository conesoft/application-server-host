using Conesoft.Files;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Conesoft.Server_Host.UI;

public partial class MainWindow : MetroWindow
{
    private readonly TrayIcon trayIcon = new();

    record Settings(string HostingPath, bool AutoStart, bool StartMinimized);

    File settingsFile;
    Settings settings;

    public MainWindow()
    {
        InitializeComponent();


        CloseAllFlyouts();

        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.ThemeChanged += (sender, e) => SetServerIconsByTheme(e.NewTheme);
        ThemeManager.Current.SyncTheme();
        SetServerIconsByTheme(ThemeManager.Current.DetectTheme());

        trayIcon.AttachToWindow(this);

        _ = SyncLogToScreen();
    }

    protected override async void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        DotNetVersion.Text = $"running on .NET {Environment.Version.Major}";

        Log.Information("loading settings ...");
        try
        {
            settingsFile = Directory.Common.User.Roaming / Assembly.GetExecutingAssembly().GetName().Name / Filename.From("Settings", "json");
            settings = await settingsFile.ReadFromJson<Settings>();
        }
        catch
        {
            Log.Error("loading failed");
        }

        settings ??= new Settings("", false, false);

        SettingsHostingPath.Text = settings.HostingPath;
        SettingsAutoStart.IsOn = settings.AutoStart;
        SettingsStartMinimized.IsOn = settings.StartMinimized;

        await SaveSettings(loaded: true);
    }

    void SetServerIconsByTheme(Theme theme)
    {
        var iconPath = Directory.From(Environment.CurrentDirectory) / "Icons";

        trayIcon.UpdateTheme(theme.BaseColorScheme);

        Icon = theme.BaseColorScheme switch
        {
            "Light" => new BitmapImage(new Uri(@"Icons\Server.Dark.ico", UriKind.Relative)),
            "Dark" => new BitmapImage(new Uri(@"Icons\Server.Light.ico", UriKind.Relative)),
            _ => new BitmapImage()
        };
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        ShowInTaskbar = WindowState != WindowState.Minimized;
        WindowStyle = WindowState != WindowState.Minimized ? WindowStyle.None : WindowStyle.ToolWindow; // hides from alt-tab
    }

    bool once = true;

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (once)
        {
            var context = Tag as App.HostingTag;
            var hosting = context.Hosting;
            hosting.OnServicesChanged += Hosting_OnServicesChanged;
            hosting.TrackServicesChanges();

            once = false;
        }
    }

    private void Hosting_OnServicesChanged(Web.Hosting.Service[] services)
    {
        var sites = services.OfType<Web.Hosting.Site>();
        var otherservices = services.Except(sites);
        var sorted = sites.GroupBy(s => s.Domain).OrderByDescending(s => s.Key);

        Dispatcher.Invoke(() =>
        {
            DataContext = new
            {
                Domains = sorted.Select(s => new
                {
                    Domain = s.Key + "LA",
                    Subdomains = s.ToArray()
                }).ToArray(),
                Services = otherservices.ToArray()
            };
        });

        trayIcon.UpdateContextMenu(sorted);
    }

    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        var target = (sender as Control).Tag as string;
        Process.Start(new ProcessStartInfo("https://" + target)
        {
            UseShellExecute = true,
        });
    }

    private void Tile_ShutDownHost_Click(object sender, RoutedEventArgs e) => Close();

    private void Tile_RebootHost_Click(object sender, RoutedEventArgs e)
    {
        ProcessStartInfo info = Application.ResourceAssembly.Location.EndsWith(".dll") switch
        {
            true => new("dotnet", Application.ResourceAssembly.Location + " " + string.Join(" ", Environment.GetCommandLineArgs())),
            false => new(Application.ResourceAssembly.Location, string.Join(" ", Environment.GetCommandLineArgs()))
        };
        info.WorkingDirectory = Environment.CurrentDirectory;
        info.UseShellExecute = true;
        Process.Start(info);
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
    {
        UseShellExecute = true,
    });

    private void Tile_OpenSettings_Click(object sender, RoutedEventArgs e) => CloseAllFlyouts(butToggleThisOne: SettingsFlyout);

    private void Tile_OpenLogFiles_Click(object sender, RoutedEventArgs e) => CloseAllFlyouts(butToggleThisOne: LogFilesFlyout);

    private void HostingPath_TextChanged(object sender, TextChangedEventArgs e) => _ = SaveSettings();
    private void AutoStart_Toggled(object sender, RoutedEventArgs e) => _ = SaveSettings();
    private void StartMinimized_Toggled(object sender, RoutedEventArgs e) => _ = SaveSettings();

    private static bool settingsLoaded;

    private async Task SaveSettings(bool loaded = false)
    {
        settingsLoaded = settingsLoaded || loaded;

        if (settingsLoaded)
        {
            settings = new Settings(SettingsHostingPath.Text, SettingsAutoStart.IsOn, SettingsStartMinimized.IsOn);

            ApplySettings(settings);

            Log.Information("saving settings ...");

            try
            {
                await settingsFile.WriteAsJson(settings, null);
            }
            catch
            {
                Log.Error("saving failed");
            }
        }
    }

    private void ApplySettings(Settings settings)
    {
        var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        var appName = Assembly.GetEntryAssembly().GetName().Name;
        var appLocation = Environment.ProcessPath;
        if (settings.AutoStart)
        {
            if (key.GetValue(appName) == null)
            {
                key.SetValue(appName, appLocation);
            }
        }
        else
        {
            if (key.GetValue(appName) != null)
            {
                key.DeleteValue(appName);
            }
        }

    }

    private async Task SyncLogToScreen()
    {
        await foreach (var files in Directory.From(@"D:\Hosting").Live().Changes())
        {
            if (files.ThereAreChanges)
            {
                try
                {
                    var scroller = LogOutput.Parent as ScrollViewer;
                    var text = (await Task.WhenAll(files.All.Where(f => f.Name == "log.txt").Select(f => ReadText(f)))).FirstOrDefault() ?? "<no log found>";
                    LogOutput.Text = text;
                    if (isAtEnd)
                    {
                        scroller.ScrollToEnd();
                    }
                }
                catch (Exception ex)
                {
                    LogOutput.Text = "<error> " + ex.Message;
                }
            }
        }
    }

    static bool isAtEnd = false;

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) => isAtEnd = (sender as ScrollViewer).VerticalOffset == (sender as ScrollViewer).ScrollableHeight;

    public async Task<string> ReadText(File file)
    {
        if (file.Exists)
        {
            using var stream = new System.IO.FileStream(file.Path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite, 0x1000, System.IO.FileOptions.SequentialScan);
            using var reader = new System.IO.StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        return default;
    }

    private void Button_OpenLogFile_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(@"D:\Hosting\log.txt") { UseShellExecute = true });
    }

    private void ContextMenu_Open_Click(object sender, RoutedEventArgs e)
    {
        var contextMenu = (sender as MenuItem);
        var site = contextMenu.Tag as Web.Hosting.Site;
        Process.Start(new ProcessStartInfo("https://" + site.FullDomain)
        {
            UseShellExecute = true,
        });
    }

    private async void ContextMenu_Restart_Click(object sender, RoutedEventArgs e)
    {
        var hosting = (Tag as App.HostingTag).Hosting;
        var menuItem = (sender as MenuItem);
        var site = menuItem.Tag as Web.Hosting.Site;

        menuItem.IsEnabled = false;

        await hosting.RestartSite(site, waitForPort: true);

        menuItem.IsEnabled = true;
    }

    private void CloseAllFlyouts(Flyout butToggleThisOne = null)
    {
        foreach (var flyout in Flyouts.FindChildren<Flyout>())
        {
            flyout.IsOpen = flyout == butToggleThisOne ? !flyout.IsOpen : false;
        }
    }
}