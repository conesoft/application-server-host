using Conesoft.Files;
using Conesoft.Server_Host.Features.Configuration.Options;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using IO = System.IO;

namespace Conesoft.Server_Host.Features.Configuration.Extensions;

static class AddHostEnvironmentInfoExtension
{
    public static WebApplicationBuilder AddHostConfigurationFiles(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddHostConfigurationToConfiguration(developmentMode: builder.Environment.IsDevelopment());

        builder.Services.ConfigureOptionsSection<HostingOptions>(section: "hosting");

        return builder;
    }

    public static WebApplicationBuilder AddHostConfigurationFiles<OptionsType>(this WebApplicationBuilder builder, string section) where OptionsType : class
    {
        builder.AddHostConfigurationFiles();

        builder.Services.ConfigureOptionsSection<OptionsType>(section);

        return builder;
    }

    private static ConfigurationManager AddHostConfigurationToConfiguration(this ConfigurationManager configuration, bool developmentMode)
    {
        var deployFile = Directory.Common.Current.FilteredFiles("Deploy.pubxml", allDirectories: true).FirstOrDefault();

        var appName = FindAppName(configuration, deployFile);
        var root = FindRoot(configuration, deployFile);

        configuration.AddAppNameToConfiguration(appName);
        configuration.AddRootToConfiguration(root);

        configuration.AddJsonFile(IO.Path.Combine(root, "Settings", "settings.json"));
        configuration.AddJsonFile(IO.Path.Combine(root, "Settings", appName + ".json"), optional: true, reloadOnChange: true);

        if (developmentMode == false)
        {
            configuration.AddJsonFile(IO.Path.Combine(root, "Settings", "hosting.json"));
        }

        return configuration;
    }

    private static string FindAppName(ConfigurationManager _, File? deployFile)
    {
        var appNameFromDeployFile = deployFile != null ? XDocument.Load(deployFile.Path).XPathSelectElement("//Name|//Domain")?.Value : null;

        var appNameFromExecutingAssemblyPath = IO.Path.GetFileName(IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        return appNameFromDeployFile ?? appNameFromExecutingAssemblyPath ?? throw new Exception("Could not find hosting:appname from Deploy.pubxml or Executing Assembly Location");
    }

    private static void AddAppNameToConfiguration(this ConfigurationManager configuration, string appName)
    {
        configuration.AddInMemoryCollection([new("hosting:appname", appName)]);
    }

    private static string FindRoot(ConfigurationManager configuration, File? deployFile)
    {
        var rootFromConfiguration = configuration["hosting:root"];

        var rootFromDeployHostingValue = deployFile switch
        {
            File => XDocument.Load(deployFile.Path).XPathSelectElement("//Hosting")?.Value switch
            {
                string path => Directory.From(path).Parent.Parent.Path,
                _ => null
            },
            _ => null
        };

        var rootFromAssemblyParentPath = File.From(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Path;

        return rootFromConfiguration ?? rootFromDeployHostingValue ?? rootFromAssemblyParentPath ?? throw new Exception("Could not find hosting:root from appseettings.json, Deploy.pubxml or Executing Assembly Location");
    }

    private static void AddRootToConfiguration(this ConfigurationManager configuration, string root)
    {
        configuration.AddInMemoryCollection([new("hosting:root", root)]);
    }
}
