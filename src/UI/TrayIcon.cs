using Conesoft.Host.Web;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace Conesoft.Host.UI
{
    public class TrayIcon
    {
        NotifyIcon notifyIcon;

        Window window;
        string theme;
        Icon IconFromTheme => theme switch
        {
            "Light" => new Icon("Server.Dark.ico"),
            "Dark" => new Icon("Server.Light.ico"),
            _ => null
        };

        public void AttachToWindow(Window window)
        {
            this.window = window;
            window.Closing += (sender, e) => notifyIcon.Visible = false;
            window.StateChanged += (sender, e) => notifyIcon.Visible = window.WindowState == WindowState.Minimized;
        }

        public void UpdateTheme(string baseColorScheme)
        {
            theme = baseColorScheme;
            if (notifyIcon != null)
            {
                notifyIcon.Icon = IconFromTheme;
                notifyIcon.ContextMenuStrip.BackColor = theme == "Dark" ? Color.Black : Color.White;
                notifyIcon.ContextMenuStrip.ForeColor = theme == "Dark" ? Color.White : Color.Black;
                notifyIcon.ContextMenuStrip.Renderer = new ToolStripProfessionalRenderer(theme == "Dark" ? new MyDarkColorTable() : new MyLightColorTable());
            }
        }

        public void UpdateContextMenu(IOrderedEnumerable<IGrouping<string, Hosting.Site>> sorted)
        {
            if (notifyIcon == null)
            {
                notifyIcon = new()
                {
                    Visible = false,
                    ContextMenuStrip = new()
                };
                UpdateTheme("Dark");
                notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("open app", null, new EventHandler((a, b) => window.WindowState = WindowState.Normal)));

                foreach (var domain in sorted)
                {
                    notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
                    foreach (var subdomain in domain)
                    {
                        notifyIcon.ContextMenuStrip.Items.Add(
                            new ToolStripMenuItem($"go to {subdomain.FullDomain}", null, new EventHandler((a, b) => Process.Start(new ProcessStartInfo($"https://{subdomain.FullDomain}")
                            {
                                UseShellExecute = true,
                            })))
                        );
                    }
                }

                notifyIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
                {
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("exit", null, new EventHandler((a, b) => System.Windows.Application.Current.Shutdown()))
                });

                notifyIcon.MouseClick += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                };
            }
        }
    }
}
