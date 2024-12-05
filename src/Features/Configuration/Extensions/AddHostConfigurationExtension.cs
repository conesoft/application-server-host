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
        var appName = deployFile switch
        {
            File file => XDocument.Load(file.Path).XPathSelectElement("//Name|//Domain")?.Value,
            _ => IO.Path.GetFileName(IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))!
        };
        configuration.AddInMemoryCollection([new("hosting:appname", appName)]);

        var rootFromConfig = configuration["hosting:root"];
        var rootFromDeployHostingValue = deployFile != null ? XDocument.Load(deployFile.Path).XPathSelectElement("//Hosting")?.Value : null;
        if (rootFromConfig is not null || rootFromDeployHostingValue is not null)
        {
            var root = rootFromConfig ?? Directory.From(rootFromDeployHostingValue!).Parent.Parent.Path;
            configuration.AddJsonFile(IO.Path.Combine(root, "Settings", "settings.json"));
            if (developmentMode == false)
            {
                configuration.AddJsonFile(IO.Path.Combine(root, "Settings", "hosting.json"));
            }

            var privateSettingsPath = IO.Path.Combine(root, "Settings", appName + ".json");
            configuration.AddJsonFile(privateSettingsPath, optional: true, reloadOnChange: true);

            return configuration;
        }
        else
        {
            throw new Exception("missing configuration for hosting:root in appsettings.json");
        }
    }
}
