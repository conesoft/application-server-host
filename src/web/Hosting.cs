using Conesoft.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Host.Web
{
    public class Hosting
    {
        public record Service(File Deployment, Process Process)
        {
            public string Name => Deployment.NameWithoutExtension;
            public virtual Directory Hosting => Deployment.Parent.Parent.Parent / Live / Deployment.Parent.Name / Deployment.NameWithoutExtension;



            public static string[] Types => new[] { "Services", "Websites" };
            public static Service FromFile(File file) => file.Parent.Name switch
            {
                "Websites" => new Site(file, null, null),
                "Services" => new Service(file, null),
                _ => null
            };
        }

        public record Site(File Deployment, Process Process, int? Port) : Service(Deployment, Process)
        {
            public string Domain => string.Join('.', Name.Split('.').TakeLast(2));
            public string Subdomain => Name.Split('.').SkipLast(2).FirstOrDefault() ?? MainDomainPart;
            public string FullDomain => Name;
            public string RelevantDomainPart => Subdomain.ToLowerInvariant() == MainDomainPart ? Domain : Subdomain;
            public override Directory Hosting => base.Hosting.Parent / Domain / Subdomain;

            static string MainDomainPart => "main";
        }

        readonly Directory root;
        readonly Dictionary<File, Service> services = new();

        public event Action<Service[]> OnServicesChanged;
        public void TrackServicesChanges() => OnServicesChanged?.Invoke(services.Values.ToArray());

        static string Deployments => "Deployments";
        static string Live => "Live";

        public Hosting(IConfiguration configuration)
        {
            root = Directory.From(configuration["hosting:root"]);
        }

        public async Task Begin(string responseUrl)
        {
            await foreach (var files in (root / Deployments).Live(allDirectories: true).Changes())
            {
                if (files.ThereAreChanges)
                {
                    Log.Information($"changes in {string.Join(" & ", files.Added.Concat(files.Changed).Concat(files.Deleted).Select(f => f.Parent.Name).Where(n => Service.Types.Contains(n)).Distinct())}");

                    foreach (var file in files.Added.Concat(files.Deleted).Concat(files.Changed).ToArray())
                    {
                        await StopDeploySite(file);
                        TrackServicesChanges();
                    }
                    foreach (var file in files.Added.Concat(files.Changed).ToArray())
                    {
                        await StartDeploySite(file, responseUrl);
                        TrackServicesChanges();
                    }
                }
            }
        }

        public void UseHostingOnApplicationBuilder(IApplicationBuilder app, string responseUrl, IHttpForwarder forwarder)
        {
            var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = true
            });

            var transformer = new CustomTransformer(); // or HttpTransformer.Default;
            var requestOptions = new ForwarderRequestConfig { Timeout = TimeSpan.FromSeconds(100) };

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/register-host", async httpContext =>
                {
                    try
                    {
                        if (httpContext.Request.Query["site"].ToString() is string site && int.Parse(httpContext.Request.Query["port"]) is int port)
                        {
                            Log.Information($"{site} -> {port}");

                            UpdatePort(site, port);

                            httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                            await httpContext.Response.WriteAsync($"registering host {site} at {port}");
                        }
                    }
                    catch (Exception e)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync($"Error Message: {e.Message}");
                    }
                });

                endpoints.Map("/{**catch-all}", async httpContext =>
                {
                    if (SiteByDomain(httpContext.Request.Host.Host) is Site site && site.Port != null)
                    {
                        await forwarder.SendAsync(httpContext, $"https://localhost:{site.Port}", httpClient, requestOptions, transformer);
                    }
                    else
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                        await httpContext.Response.WriteAsync($"");
                    }
                });
            });

            var _ = Begin(responseUrl);
        }

        Site SiteByDomain(string domain) => services.Values.OfType<Site>().Where(p => p.FullDomain == domain.ToLowerInvariant()).FirstOrDefault();

        void UpdatePort(string domain, int port)
        {
            if (SiteByDomain(domain) is Site site)
            {
                services[site.Deployment] = site with { Port = port };
            }
        }

        async Task StartDeploySite(File file, string responseUrl)
        {
            await Task.Run(() =>
            {
                if (Service.FromFile(file) is Service service)
                {
                    Log.Information($"\tstart {file}");

                    file.WaitTillReady();

                    service.Hosting.Parent.Create();
                    service.Deployment.AsZip().ExtractTo(service.Hosting);
                    service = service with { Process = RunHosted(service.Hosting.Filtered("*.exe", allDirectories: false).FirstOrDefault(), responseUrl) };
                    services[file] = service;
                }
            });
        }



        async Task StopDeploySite(File file)
        {
            await Task.Run(() =>
            {
                if ((services.GetValueOrDefault(file) ?? Service.FromFile(file)) is Service service)
                {
                    if (service.Process is Process process)
                    {
                        Log.Information($"\tstop {file}");

                        process.Kill();
                        process.WaitForExit();
                    }
                    services.Remove(file);
                    service.Hosting.Delete();

                    if (!service.Hosting.Parent.AllFiles.Any() && service.Hosting.Parent.Parent != root / Live)
                    {
                        service.Hosting.Parent.Delete();
                    }
                }
            });
        }

        static Process RunHosted(File file, string responseUrl)
        {
            var start = new ProcessStartInfo(file.Path, $"--urls=https://*:0/ --conesoft-host-register={responseUrl + "register-host"}")
            {
                WorkingDirectory = file.Parent.Path,
                CreateNoWindow = true,
            };

            return FromStackOverflow.ChildProcessTracker.Track(Process.Start(start));
        }
    }
}
