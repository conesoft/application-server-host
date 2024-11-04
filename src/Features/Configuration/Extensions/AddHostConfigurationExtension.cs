using Conesoft.Files;
using Conesoft.Server_Host.Features.Configuration.Options;

namespace Conesoft.Server_Host.Features.Configuration.Extensions;

static class AddHostEnvironmentInfoExtension
{
    public static WebApplicationBuilder AddHostConfigurationFiles(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddHostConfigurationToConfiguration(developmentMode: builder.Environment.IsDevelopment());

        builder.Services.ConfigureOptionsSection<HostingOptions>(section: "hosting");
        builder.Services.ConfigureOptionsSection<DeploymentOptions>(section: "PropertyGroup");

        return builder;
    }

    private static IConfigurationBuilder AddHostConfigurationToConfiguration(this ConfigurationManager configuration, bool developmentMode)
    {
        if (configuration["hosting:root"] is string root)
        {
            if (developmentMode == false)
            {
                configuration.AddJsonFile(System.IO.Path.Combine(root, "Settings", "hosting.json"));
            }
            configuration.AddJsonFile(System.IO.Path.Combine(root, "Settings", "settings.json"));
        }
        if (Directory.Common.Current.FilteredFiles("Deploy.pubxml", allDirectories: true).FirstOrDefault() is File file)
        {
            configuration.AddXmlFile(file.Path);
        }
        return configuration;
    }
}
