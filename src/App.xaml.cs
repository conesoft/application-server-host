using Conesoft.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
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

            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var root = Directory.From(configuration["hosting:root"]);

            var log = root / Filename.From("log", "txt");
            System.IO.File.WriteAllText(log.Path, "");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(log.Path, buffered: false, flushToDiskInterval: TimeSpan.FromMilliseconds(1))
                .CreateLogger();

            Log.Information("App has started");

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

            Log.Information("App has ended");
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