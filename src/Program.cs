using Conesoft.Hosting;
using Conesoft.Server_Host.Features.ActivePorts.Extensions;
using Conesoft.Server_Host.Features.ActiveProcesses.Extensions;
using Conesoft.Server_Host.Features.Certificates;
using Conesoft.Server_Host.Features.Certificates.Extensions;
using Conesoft.Server_Host.Features.Certificates.Options;
using Conesoft.Server_Host.Features.Deployments.Extensions;
using Conesoft.Server_Host.Features.MediatorService.Extensions;
using Conesoft.Server_Host.Features.ProxyTarget;
using Conesoft.Server_Host.Features.ProxyTarget.Extensions;
using Conesoft.Server_Host.Features.StateWriter.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddHostConfigurationFiles(configurator =>
    {
        configurator.Add<LetsEncryptConfiguration>("letsencrypt");
    })
    .AddHostEnvironmentInfo()
    .AddLoggingService()

    .AddMediator()

    .AddStateWriter()
    .UseCertificateSelector<CertificateSelector>()
    .AddProxyTargetSelection<SelectActiveTarget>()
    .AddActiveProcesses()
    .AddActivePorts()
    .AddDeploymentWatcher()
    ;

builder.Services
    .AddHttpClient()
    .AddHttpClient("shorttimeout", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(1);
    });

var app = builder.Build();

app.UseLoggingServiceOnRequests();
app.MapGet("/server/shutdown", (IHostApplicationLifetime lifetime) =>
{
    Log.Information("shutting down server");
    lifetime.StopApplication();
});
app.MapProxyTargets();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();


app.MapFallback(() => Results.Text("404 Not Found: Wrong Domain", statusCode: 404));

app.Run();