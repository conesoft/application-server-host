using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActiveProcesses.Services;
using Conesoft.Server_Host.Features.Deployments.Messages;
using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Serilog;
using System.Diagnostics;

namespace Conesoft.Server_Host.Features.Deployments.Services;

class HostDeploymentHandler(HostEnvironment environment, ActiveProcessesService activeProcesses, IHostApplicationLifetime app) :
    IListener<StartDeployment>,
    IListener<StopDeployment>
{
    private bool isUpdating = false;

    void IListener<StartDeployment>.Listen(StartDeployment message)
    {
        if(isUpdating)
        {
            return;
        }
        Log.Information("Starting Host Deployment for {message}", message.Source.NameWithoutExtension);
        var target = environment.Global.Live / message.Source.Parent.Name / message.Source.NameWithoutExtension;

        Log.Information("Current Deployment Timestamp {time}", target.Info.LastWriteTimeUtc);
        Log.Information("New Deployment Timestamp     {time}", message.Source.Info.LastWriteTimeUtc);

        if(message.Source.WaitTillReady() == false)
        {
            Log.Information("New deployment File wasn't ready");
        }

        if (message.Source.Info.LastWriteTimeUtc > target.Info.LastWriteTimeUtc)
        {
            Log.Information("New Version for Host found");
            isUpdating = true;
            try
            {
                // TODO: fill script parameters, write to temporary .cmd, run temporary .cmd
                var oldExe = Environment.ProcessPath ?? throw new Exception("current executable path can't be determined");
                var newExe = message.Source.AsZip().Entries.FirstOrDefault(e => e.Name.EndsWith(".exe") && e.Path == "/") ?? throw new Exception("deployment package does not contain an executable");
                var script = GenerateScript(
                    sourceZipFile: message.Source,
                    destinationDirectory: target,
                    sourceExecutable: Files.File.From(Environment.ProcessPath),
                    destinationExecutable: target / Filename.FromExtended(newExe.Name)
                );
                Log.Information("script generated: {script}", script);
                var updateFile = Files.Directory.From(Path.GetTempPath()) / Filename.From($"conesoft updater - {Guid.NewGuid()}", "cmd");
                Log.Information("update file planneed at {filepath}", updateFile.Path);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                updateFile.WriteText(script).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

                Log.Information("update file generated at {filepath}", updateFile.Path);
                Process.Start(new ProcessStartInfo(updateFile.Path)
                {
                    WorkingDirectory = Environment.CurrentDirectory,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                app.StopApplication();
            }
            catch (Exception e)
            {
                Log.Error("Could not extract to {directory} due to {exception}", target, e);
                isUpdating = false;
            }
        }
        else
        {
            Log.Information("Detected Version is not new");
            isUpdating = false;
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        Log.Information("Ignoring Stop Message for {message}", message.Source.NameWithoutExtension);
    }

    static string GenerateScript(Files.File sourceZipFile, Files.Directory destinationDirectory, Files.File sourceExecutable, Files.File destinationExecutable) =>
        $"""
        @ECHO OFF
        ECHO waiting for application '{sourceExecutable.NameWithoutExtension}' to end

        :LOOP
        tasklist | find /i "{sourceExecutable.Name}" >nul 2>&1
        IF ERRORLEVEL 1 (
            GOTO CONTINUE
        ) ELSE (
            timeout /T 1 /nobreak
            GOTO LOOP
        )

        ECHO deploying '{sourceZipFile.Name}' to '{destinationDirectory.Path}'

        :CONTINUE
        rd /s /q "{destinationDirectory.Path}"
        md "{destinationDirectory.Path}"
        tar -xf "{sourceZipFile.Path}" -C "{destinationDirectory.Path}"
        {destinationDirectory.Path[..2]}
        cd "{destinationDirectory.Path}"
        start /b cmd /c "{destinationExecutable.Path}"
        del %0
        """;
}