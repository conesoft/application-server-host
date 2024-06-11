using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft.Server_Host.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Conesoft.Server_Host;

public partial class App : Application
{
    public record HostingTag(Web.Hosting Hosting);
    private IHost? host;
    private LoggingExtensions.HostedLoggingExtensionWrapper? wrapper;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);


        this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var root = Directory.From(configuration["hosting:root"]!);
        var log = root / "Logs" / Filename.From("Application - Host - ", "txt");

        wrapper = LoggingExtensions.AddLogging(log.Path, "Conesoft Host");
        wrapper.Start();

        Log.Information("app current directory: {dir}", Environment.CurrentDirectory);

        //// DIE, ALREADY OPEN APPS, DIE!!!
        var currentProcess = Process.GetCurrentProcess();
        var processesToKill = Process.GetProcessesByName(currentProcess.ProcessName).Where(p => p.Id != currentProcess.Id).ToList();
        if (processesToKill.Count != 0)
        {
            foreach (var p in processesToKill)
            {
                p.Kill();
            }
            await Task.Delay(1000);
        }

        if (Environment.CurrentDirectory == Environment.SystemDirectory)
        {
            var executableDirectory = File.From(Environment.ProcessPath!).Parent.Path;
            Environment.CurrentDirectory = executableDirectory;
            Log.Information("Remapping 'Current Directory' from '{old}' to '{new}'", Environment.CurrentDirectory, executableDirectory);
        }

        Log.Information("Starting periodic garbage collection");
        var _ = GarbageCollect.Every(TimeSpan.FromMinutes(5));

        Log.Information("Starting web service");
        try
        {
            host = await Web.Program.StartWebService(Environment.GetCommandLineArgs());
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.ToString());
        }
    }

    private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception.Message);
        Log.Error(e.Exception.ToString());

        throw e.Exception;
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
        MainWindow.Tag = new HostingTag((host!.Services.GetService(typeof(Web.Hosting)) as Web.Hosting)!);
    }
}