using Conesoft.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Service.Proxy;

namespace Conesoft.Host.Web
{
    public class Hosting
    {
        public record Site(File Deployment, Process Process, string Domain, string Subdomain, int? Port)
        {
            public string FullDomain => (Subdomain.ToLowerInvariant() == "main" ? Domain : $"{Subdomain}.{Domain}").ToLowerInvariant();
            public string RelevantDomainPart => Subdomain.ToLowerInvariant() == "main" ? Domain : Subdomain;
        };

        readonly Directory root;
        readonly Dictionary<File, Site> sites = new();

        public event Action<IReadOnlyDictionary<File, Site>> OnSitesChanged;
        public void TrackSiteChanges() => OnSitesChanged?.Invoke(sites);

        Directory Deployments => root / "Deployments" / "Websites";
        Directory Hostings => root / "Websites";

        public Hosting(IConfiguration configuration)
        {
            root = Directory.From(configuration["hosting:root"]);
        }

        public async Task Begin(string responseUrl)
        {
            await foreach (var files in Deployments.Live().Changes())
            {
                foreach (var file in files.Added.Concat(files.Deleted).Concat(files.Changed).ToArray())
                {
                    StopDeploy(file);
                }
                foreach (var file in files.Added.Concat(files.Changed).ToArray())
                {
                    StartDeploy(file, responseUrl);
                }
                OnSitesChanged?.Invoke(sites);
            }
        }

        public void UseHostingOnApplicationBuilder(IApplicationBuilder app, string responseUrl, IHttpProxy proxy)
        {
            var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = true
            });

            var transformer = new CustomTransformer(); // or HttpTransformer.Default;
            var requestOptions = new RequestProxyOptions { Timeout = TimeSpan.FromSeconds(100) };

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/register-host", async httpContext =>
                {
                    try
                    {
                        if(httpContext.Request.Query["site"].ToString() is string site && int.Parse(httpContext.Request.Query["port"]) is int port)
                        {
                            var _ = (root / Filename.From("log", "txt")).AppendLine($"{site} -> {port}");
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
                    if (SiteByDomain(httpContext.Request.Host.Host) is (File file, Site site) && site.Port != null)
                    {
                        await proxy.ProxyAsync(httpContext, $"https://localhost:{site.Port}", httpClient, requestOptions, transformer);
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

        (File file, Site site) SiteByDomain(string domain) => sites.Where(p => p.Value.FullDomain == domain.ToLowerInvariant()).Select(p => (file: p.Key, site: p.Value)).FirstOrDefault();

        void UpdatePort(string domain, int port)
        {
            if (SiteByDomain(domain) is (File file, Site site))
            {
                sites[file] = site with { Port = port };
            }
        }

        void StartDeploy(File file, string responseUrl)
        {
            var segments = GetDomainSegments(file);
            Site site = new(file, null, segments.domain, segments.subdomain, null);
            var hosting = Hostings / site.Domain / site.Subdomain;
            hosting.Parent.Create();
            site.Deployment.AsZip().ExtractTo(hosting);
            site = site with { Process = RunHosted(hosting.Filtered("*.exe", allDirectories: false).FirstOrDefault(), responseUrl) };
            sites[file] = site;
        }

        void StopDeploy(File file)
        {
            if (sites.GetValueOrDefault(file) is Site site)
            {
                var hosting = Hostings / site.Domain / site.Subdomain;
                site.Process.Kill();
                site.Process.WaitForExit();
                sites.Remove(file);
                System.IO.Directory.Delete(hosting.Path, true);
                if(!hosting.Parent.AllFiles.Any())
                {
                    hosting.Parent.Delete();
                }
            }
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

        static (string domain, string subdomain) GetDomainSegments(File file)
        {
            var domainSegments = file.NameWithoutExtension.Split('.').ToArray();
            var domain = $"{domainSegments.SkipLast(1).Last()}.{domainSegments.Last()}";
            var subdomain = domainSegments.Length == 3 ? domainSegments.First() : "main";

            return (domain, subdomain);
        }
    }
}
