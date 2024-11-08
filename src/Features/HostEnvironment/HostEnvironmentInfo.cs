﻿using Conesoft.Files;
using Conesoft.Server_Host.Features.Configuration.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Conesoft.Server_Host.Features.HostEnvironment;

class HostEnvironmentInfo
{
    public enum HostingType { Application, Service, Website }

    public string Name { get; private init; }
    public HostingType Type { get; private set; }
    public Directory Root { get; private init; }
    public bool IsInHostedEnvironment { get; private init; }

    public HostEnvironmentInfo(IOptions<HostingOptions> hostingOptions, IOptions<DeploymentOptions> deploymentOptions)
    {
        if (hostingOptions.Value.Root == "" && deploymentOptions.Value.Hosting == "")
        {
            throw new ApplicationException("Hosting Path Configuration not found");
        }
        if (Assembly.GetExecutingAssembly().GetName().Name == null)
        {
            throw new ApplicationException("Application Name Configuration not found");
        }

        Name = Assembly.GetExecutingAssembly().GetName().Name!;

        Root =
            hostingOptions.Value.Root != "" ?
            Directory.From(hostingOptions.Value.Root) :
            Directory.From(deploymentOptions.Value.Hosting).Parent.Parent
            ;

        Type =
            deploymentOptions.Value.Hosting != "" ?
            deploymentOptions.Value.Domain != "" ? HostingType.Website : HostingType.Service :
            HostingType.Application
            ;

        IsInHostedEnvironment = Assembly.GetExecutingAssembly().Location.StartsWith(
            System.IO.Path.TrimEndingDirectorySeparator(Root.Path) + System.IO.Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase
        );

        Root = IsInHostedEnvironment == false ? Root / "Test Environment" : Root;
    }
}