using Conesoft.Files;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Conesoft.Host.UI
{
    public partial class MainWindow : MetroWindow
    {
        private readonly TrayIcon trayIcon = new();

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();
            ThemeManager.Current.ThemeChanged += (sender, e) => SetServerIconsByTheme(e.NewTheme);
            SetServerIconsByTheme(ThemeManager.Current.DetectTheme());

            trayIcon.AttachToWindow(this);
        }

        void SetServerIconsByTheme(Theme theme)
        {
            Icon = theme.BaseColorScheme switch
            {
                "Light" => new BitmapImage(new Uri("Icons/Server.Dark.png", UriKind.Relative)),
                "Dark" => new BitmapImage(new Uri("Icons/Server.Light.png", UriKind.Relative)),
                _ => new BitmapImage()
            };
            trayIcon.UpdateTheme(theme.BaseColorScheme);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            ShowInTaskbar = WindowState != WindowState.Minimized;
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

            DataContext = new
            {
                Domains = sorted.Select(s => new
                {
                    Domain = s.Key,
                    Subdomains = s.ToArray()
                }),
                Services = otherservices.Select(s => new
                {
                    Name = s.Name.ToLowerInvariant()
                })
            };

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

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true,
        });
    }
}
