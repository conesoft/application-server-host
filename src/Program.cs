﻿using Conesoft.Server_Host.Features.ActivePorts.Extensions;
using Conesoft.Server_Host.Features.ActiveProcesses.Extensions;
using Conesoft.Server_Host.Features.Certificates;
using Conesoft.Server_Host.Features.Certificates.Extensions;
using Conesoft.Server_Host.Features.Configuration.Extensions;
using Conesoft.Server_Host.Features.Deployments.Extensions;
using Conesoft.Server_Host.Features.DesktopApplication.Extensions;
using Conesoft.Server_Host.Features.GarbageCollection.Extensions;
using Conesoft.Server_Host.Features.HostEnvironment.Extensions;
using Conesoft.Server_Host.Features.Logging.Extensions;
using Conesoft.Server_Host.Features.Mediator.Extensions;
using Conesoft.Server_Host.Features.ProxyTarget;
using Conesoft.Server_Host.Features.ProxyTarget.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddHostConfigurationFiles();
builder.AddHostEnvironmentInfo();
builder.AddLoggingService();
builder.AddGarbageCollection();

builder.AddDesktopApplication();

builder.AddMediator();

builder.UseCertificateSelector<CertificateSelector>();
builder.AddProxyTargetSelection<SelectActiveTarget>();
builder.AddActiveProcesses();
builder.AddActivePorts();
builder.AddDeploymentWatcher();

var app = builder.Build();

app.UseLoggingServiceOnRequests();
app.MapProxyTargets();
app.MapGet("/{**catchall}", (string? catchall) => Results.Text("404 Not Found: Wrong Domain", statusCode: 404));

app.Run();