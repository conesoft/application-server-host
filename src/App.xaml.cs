using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Conesoft.Host
{
    public partial class App : Application
    {
        public record HostingTag(Web.Hosting Hosting);
        private IHost host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //// DIE, ALREADY OPEN APPS, DIE!!!
            var currentProcess = Process.GetCurrentProcess();
            foreach (var p in Process.GetProcessesByName(currentProcess.ProcessName).Where(p => p.Id != currentProcess.Id))
            {
                p.Kill();
            }

            host = await Web.Program.StartWebService(Environment.GetCommandLineArgs());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            // DIE APP, DIE!!!
            Process.GetCurrentProcess().Kill();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            MainWindow.Tag = new HostingTag(host.Services.GetService(typeof(Web.Hosting)) as Web.Hosting);
        }
    }
}