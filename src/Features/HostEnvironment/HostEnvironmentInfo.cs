using Conesoft.Files;
using Conesoft.Server_Host.Features.Configuration.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Conesoft.Server_Host.Features.HostEnvironment;

class HostEnvironmentInfo
{
    public enum HostingType { Application, Service, Website }

    public Environment Environment { get; private init; }

    public Directories Global { get; private init; }
    public Directories Local { get; private init; }

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

        var name = Assembly.GetExecutingAssembly().GetName().Name!;

        var root =
            hostingOptions.Value.Root != "" ?
            Directory.From(hostingOptions.Value.Root) :
            Directory.From(deploymentOptions.Value.Hosting).Parent.Parent
            ;

        var type =
            deploymentOptions.Value.Hosting != "" ?
            deploymentOptions.Value.Domain != "" ? HostingType.Website : HostingType.Service :
            HostingType.Application
            ;

        var isInHostedEnvironment = Assembly.GetExecutingAssembly().Location.StartsWith(
            System.IO.Path.TrimEndingDirectorySeparator(Root.Path) + System.IO.Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase
        );

        this.Environment = new(name, type, root, isInHostedEnvironment);

        // TODO: Implment Global and Local
    }

    public record Environment(string Name, HostingType Type, Directory Root, bool IsInHostedEnvironment);
    public record Directories(Directory Deployment, Directory Live, Directory Settings, Directory Storage);
}
