namespace Conesoft.Server_Host.Features.Configuration.Options;

class HostingOptions()
{
    [ConfigurationKeyName("root")]
    public string Root { get; init; } = "";

    [ConfigurationKeyName("certificate-password")]
    public string CertificatePassword { get; init; } = "";
}
