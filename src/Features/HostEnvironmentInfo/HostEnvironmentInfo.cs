using Conesoft.Files;
using Conesoft.Server_Host.Features.Configuration.Options;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Conesoft.Server_Host.Features.HostEnvironmentInfo;

class HostEnvironment
{
    public record Directories(Directory Deployments, Directory Live, Directory Storage);

    public string ApplicationName { get; private init; }
    public bool IsInHostedEnvironment { get; private init; }
    public Directory Root { get; private init; }
    public Directories Global { get; private init; }
    public Directories Local { get; private init; }

    public HostEnvironment(IOptions<HostingOptions> hostingOptions)
    {
        if (hostingOptions.Value.Root == "")
        {
            throw new ApplicationException("Hosting Path Configuration not found");
        }

        var name = hostingOptions.Value.AppName;

        var root = Directory.From(hostingOptions.Value.Root);

        var isInHostedEnvironment = Assembly.GetExecutingAssembly().Location.StartsWith(
            System.IO.Path.TrimEndingDirectorySeparator(root.Path) + System.IO.Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase
        );

        ApplicationName = name;
        Root = root;
        IsInHostedEnvironment = isInHostedEnvironment;
        Global = new(Deployments: root / "Deployments", Live: root / "Live", Storage: root / "Storage");
        Local = new(Global.Deployments / name, Global.Live / name, Global.Storage / name);
    }
}