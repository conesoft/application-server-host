namespace Conesoft.Server_Host.Features.Certificates.Options;

class LetsEncryptConfiguration
{
    [ConfigurationKeyName("certificate-password")] public string CertificatePassword { get; set; } = "";
}
