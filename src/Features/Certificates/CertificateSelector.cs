﻿using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft.Server_Host.Features.Certificates.Interfaces;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using Serilog;
using System.Security.Cryptography.X509Certificates;

namespace Conesoft.Server_Host.Features.Certificates;

class CertificateSelector(HostEnvironment environment, IOptions<HostingOptions> options) : BackgroundService, ISelectCertificate
{
    readonly X509Certificate2 @default = CertificateLoader.LoadFromStoreCert("localhost", "My", StoreLocation.CurrentUser, allowInvalid: true);
    Dictionary<string, X509Certificate2> certificates = [];

    public X509Certificate2 CertificateFor(string domain) => certificates.GetValueOrDefault(domain.Replace(".localhost", ""), @default);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var root = environment.Global.Storage / "Certificates";
        root.Create();
        var password = options.Value.CertificatePassword;

        await foreach (var _ in root.Live(cancellation: stoppingToken))
        {
            certificates = root.Files.ToDictionary(c => c.NameWithoutExtension, c => X509CertificateLoader.LoadPkcs12FromFile(c.Path, password));
            Log.Information("Active Certificates: {certificates}", certificates.Keys);
        }
    }
}
