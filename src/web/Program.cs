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

namespace Conesoft.Host.Web
{
    public class Program
    {
        public static async Task<IHost> StartWebService(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.StartAsync();
            return host;
        }

        private static Dictionary<string, X509Certificate2> certificates = new();

        static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureWebHost(webBuilder =>
            {
                var urls = (all: webBuilder.GetSetting(WebHostDefaults.ServerUrlsKey).Split(';'), http: "", https: "");
                urls.http = urls.all.FirstOrDefault(u => u.StartsWith("http:"));
                urls.https = urls.all.FirstOrDefault(u => u.StartsWith("https:"));

                var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                var root = Directory.From(configuration["hosting:root"]);

                configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile((root / "Settings" / Filename.From("hosting", "json")).Path).Build();
                var password = configuration["hosting:certificate-password"];

                var certificatesPath = root / "Storage" / "Websites" / "Certificates";
                _ = Task.Run(async () =>
                {
                    await foreach (var files in certificatesPath.Live().Changes())
                    {
                        if(files.Added.Any() || files.Deleted.Any() || files.Changed.Any())
                        {
                            Log.Information("loading certificates");
                            certificates = files.All.ToDictionary(c => c.NameWithoutExtension, c => new X509Certificate2(c.Path, password));
                        }
                    }
                });

                webBuilder.UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, new Uri(urls.https).Port, listenOptions =>
                    {
                        listenOptions.UseHttps(httpsOptions =>
                        {
                            httpsOptions.ServerCertificateSelector = (context, dnsName) =>
                            {
                                var domain = dnsName.Replace(".localhost", "");
                                return certificates.ContainsKey(domain) ? certificates[domain] : null;
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
}