using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace Conesoft.Server_Host.UI;

public class TrayIcon
{
    NotifyIcon? notifyIcon;

    readonly Window window;
    string theme;
    Icon? IconFromTheme => theme switch
    {
        "Light" => new Icon("Icons/Server.Dark.ico"),
        "Dark" => new Icon("Icons/Server.Light.ico"),
        _ => null
    };

    public TrayIcon(Window window)
    {
        this.window = window;
        this.theme = "Dark";

        window.Closing += (sender, e) =>
        {
            if(notifyIcon != null)
            {
                notifyIcon.Visible = false;
            }
        };
        window.StateChanged += (sender, e) =>
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = window.WindowState == WindowState.Minimized;
            }
        };
        SystemEvents.DisplaySettingsChanged += RefreshIcon;
        SystemEvents.PowerModeChanged += RefreshIcon;
        SystemEvents.SessionSwitch += RefreshIcon;
        SystemEvents.UserPreferenceChanged += RefreshIcon;
    }

    private void RefreshIcon(object? sender, EventArgs e)
    {
        if (notifyIcon != null)
        {
            notifyIcon.Icon = IconFromTheme;
        }
    }

    public void UpdateTheme(string baseColorScheme)
    {
        theme = baseColorScheme;
        if (notifyIcon != null)
        {
            notifyIcon.Icon = IconFromTheme;

            if (notifyIcon.ContextMenuStrip != null)
            {
                notifyIcon.ContextMenuStrip.BackColor = theme == "Dark" ? Color.Black : Color.White;
                notifyIcon.ContextMenuStrip.ForeColor = theme == "Dark" ? Color.White : Color.Black;
                notifyIcon.ContextMenuStrip.Renderer = new ToolStripProfessionalRenderer(theme == "Dark" ? new MyDarkColorTable() : new MyLightColorTable());
            }
        }
    }

    public void UpdateContextMenu(IOrderedEnumerable<IGrouping<string, Web.Hosting.Site>> sorted)
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