using Conesoft.Server_Host.Features.Certificates.Interfaces;
using Serilog;

namespace Conesoft.Server_Host.Features.Certificates.Extensions;

static class AddDesktopApplicationExtensions
{
    public static WebApplicationBuilder UseCertificateSelector<T>(this WebApplicationBuilder builder) where T : class, IHostedService, ISelectCertificate
    {
        builder.WebHost.UseCertificateSelector();
        builder.Services.AddSingleton<T>();
        builder.Services.AddTransient<ISelectCertificate>(s => s.GetRequiredService<T>());
        builder.Services.AddHostedService(s => s.GetRequiredService<T>());
        return builder;
    }

    public static IWebHostBuilder UseCertificateSelector(this IWebHostBuilder app)
    {
        return app.ConfigureKestrel(serverOptions =>
        {
            Log.Information("Configuring Kestrel Certificate Selector");
            var certificateSelector = serverOptions.ApplicationServices.GetRequiredService<ISelectCertificate>();
            serverOptions.Limits.KeepAliveTimeout = Timeout.InfiniteTimeSpan;
            serverOptions.ConfigureEndpointDefaults(options =>
            {
                options.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
            });
            serverOptions.ConfigureHttpsDefaults(httpOptions => httpOptions.ServerCertificateSelector = (_, name) =>
            {
                return name != null ? certificateSelector.CertificateFor(name) : null;
            });
        });
    }
}