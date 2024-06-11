using Conesoft.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Conesoft.Server_Host.Web;

public class Program
{
    public static async Task<IHost> StartWebService(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.StartAsync();
        return host;
    }

    private static Dictionary<string, X509Certificate2> certificates = [];

    static IHostBuilder CreateHostBuilder(string[] args) =>
    Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
        .ConfigureWebHost(webBuilder =>
        {
            Log.Information("configuring web host");

            var defaults = webBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

            var urls = new
            {
                http = defaults?.Split(";").FirstOrDefault(u => u.StartsWith("http:")) ?? "http://localhost:80",
                https = defaults?.Split(";").FirstOrDefault(u => u.StartsWith("https:")) ?? "https://localhost:443"
            };

            Log.Information("urls: http = {http}, https = {https}", urls.http, urls.https);

            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var rootValue = configuration["hosting:root"];
            if(rootValue == null)
            {
                throw new NullReferenceException("Required setting 'hosting:root' not found in appsettings.json");
            }
            var root = Directory.From(rootValue);

            configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile((root / "Settings" / Filename.From("hosting", "json")).Path).Build();
            var password = configuration["hosting:certificate-password"];

            var certificatesPath = root / "Storage" / "Certificates";
            _ = Task.Run(async () =>
            {
                await foreach (var entries in certificatesPath.Live().Changes())
                {
                    if (entries.ThereAreChanges)
                    {
                        Log.Information("loading certificates");
                        try
                        {
                            certificates = entries.All.Files().ToDictionary(c => c.NameWithoutExtension, c => new X509Certificate2(c.Path, password));
                        }
                        catch (Exception)
                        {
                            Log.Information("failed to load certificates");
                        }
                        Log.Information("loaded {count} certificates", certificates.Count);
                    }
                }
            });

            Log.Information("using kestrel for public https");
            webBuilder.UseKestrel(options =>
            {
                options.Limits.KeepAliveTimeout = TimeSpan.MaxValue;

                options.Listen(IPAddress.Any, new Uri(urls.https).Port, listenOptions =>
                {
                    Log.Information("set up port {port}", new Uri(urls.https).Port);
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificateSelector = (context, dnsName) =>
                        {
                            if (dnsName != null)
                            {
                                var domain = dnsName.Replace(".localhost", "");
                                Log.Information($"selecting certificate for {domain}");
                                return certificates.ContainsKey(domain) ? certificates[domain] : null;
                            }
                            return null;
                        };
                    });
                });
                options.Listen(IPAddress.Any, new Uri(urls.http).Port);
            });
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
}