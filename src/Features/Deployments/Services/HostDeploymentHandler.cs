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
    async void IListener<StartDeployment>.Listen(StartDeployment message)
    {
        Log.Information("Starting Host Deployment for {message}", message.Source.NameWithoutExtension);
        var target = environment.Global.Live / message.Source.Parent.Name / message.Source.NameWithoutExtension;


        if (message.Source.Info.LastWriteTimeUtc > target.Info.LastWriteTimeUtc)
        {
            Log.Information("New Version for Host found");
            try
            {
                // TODO: fill script parameters, write to temporary .cmd, run temporary .cmd
                var script = GenerateScript(null, null, null, null);
                var updateFile = Files.Directory.From(Path.GetTempPath()) / Filename.From($"conesoft updater - {Guid.NewGuid()}", "cmd");
                await updateFile.WriteText(script);
                Process.Start(updateFile.Path);

                app.StopApplication();
            }
            catch (Exception e)
            {
                Log.Error("Could not extract to {directory} due to {exception}", target, e);
            }

            if (target.FilteredFiles("*.exe", allDirectories: false).FirstOrDefault() is Files.File executable)
            {
                var start = new ProcessStartInfo(executable.Path)
                {
                    WorkingDirectory = target.Path,
                    Arguments = "-deploy-with-processes " + string.Join(" ", activeProcesses.Services.Values.Select(p => p.Process.Id)),
                    CreateNoWindow = true
                };
                Process.Start(start);
            }
        }
        else
        {
            Log.Information("Detected Version is not new");
        }
    }

    void IListener<StopDeployment>.Listen(StopDeployment message)
    {
        Log.Information("Ignoring Stop Message for {message}", message.Source.NameWithoutExtension);
    }

    static string GenerateScript(Files.File sourceZipFile, Files.Directory destinationDirectory, Files.File sourceExecutable, Files.File destinationExecutable) => $"""
            :LOOP
            tasklist | find / i "{sourceExecutable.Name}" > nul 2 > &1
            IF ERRORLEVEL 1 (
                    GOTO CONTINUE
            ) ELSE (
                ECHO waiting till application ends
                Timeout / T 5 / Nobreak
                GOTO LOOP
            )
            :CONTINUE
            rd /s /q "{destinationDirectory.Path}"
            md "{destinationDirectory.Path}"
            tar -xf "{sourceZipFile.Path}" -C "{destinationDirectory.Path}"
            "{destinationExecutable.Path}"
""";
}