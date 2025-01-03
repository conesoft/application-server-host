using Conesoft.Server_Host.Features.ActivePorts.Extensions;
using Conesoft.Server_Host.Features.ActiveProcesses.Extensions;
using Conesoft.Server_Host.Features.Certificates;
using Conesoft.Server_Host.Features.Certificates.Extensions;
using Conesoft.Server_Host.Features.Deployments.Extensions;
using Conesoft.Server_Host.Features.MediatorService.Extensions;
using Conesoft.Server_Host.Features.ProxyTarget;
using Conesoft.Server_Host.Features.ProxyTarget.Extensions;
using Conesoft.Server_Host.Features.SelfDeployment.Extensions;
using Conesoft.Server_Host.Features.WebApplication.Extensions;
using Conesoft.Hosting;
using Conesoft.Server_Host.Features.TrayIcon.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddHostConfigurationFiles()
    .AddHostEnvironmentInfo()
    .AddLoggingService()
    
    .AddSelfDeploymentHandling()
    
    .AddMediator()
    
    .AddWebApplication()
    .AddTrayIcon()
    .UseCertificateSelector<CertificateSelector>()
    .AddProxyTargetSelection<SelectActiveTarget>()
    .AddActiveProcesses()
    .AddActivePorts()
    .AddDeploymentWatcher()
    ;

builder.Services
    .AddHttpClient();

var app = builder.Build();

app.UseLoggingServiceOnRequests();
app.MapWebApplication();
app.MapProxyTargets();
app.MapGet("/{**catchall}", (string? catchall) => Results.Text("404 Not Found: Wrong Domain", statusCode: 404));

app.Run();