using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft.Server_Host.Features.Certificates.Interfaces;
using Conesoft.Server_Host.Features.Certificates.Options;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Conesoft.Server_Host.Features.Certificates;

class CertificateSelector(HostEnvironment environment, IOptions<LetsEncryptConfiguration> options) : IHostedService, ISelectCertificate
{
    readonly X509Certificate2 @default = CertificateLoader.LoadFromStoreCert("localhost", "My", StoreLocation.CurrentUser, allowInvalid: true);
    Dictionary<string, X509Certificate2> certificates = [];
    CancellationTokenSource? cancellationTokenSource;

    public X509Certificate2 CertificateFor(string domain) => certificates.GetValueOrDefault(domain.Replace(".localhost", ""), @default);

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        var root = environment.Global.Storage / "host" / "certificates";
        root.Create();
        cancellationTokenSource = root.Live(() =>
        {
            certificates = root.Files.ToDictionary(c => c.NameWithoutExtension, c => X509CertificateLoader.LoadPkcs12FromFile(c.Path, options.Value.CertificatePassword));
        });
        return Task.CompletedTask;
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken) => await (cancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
}
