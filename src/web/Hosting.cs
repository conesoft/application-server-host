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

namespace Conesoft.Server_Host.Web;

public class Hosting
{
    public record Service(File Deployment, Process Process)
    {
        public string Name => Deployment.NameWithoutExtension.ToLowerInvariant();
        public virtual Directory Hosting => Deployment.Parent.Parent.Parent / Live / Deployment.Parent.Name / Deployment.NameWithoutExtension;



        public static string[] Types => new[] { "Services", "Websites" };
        public static Service FromFile(File file) => file.Parent.Name switch
        {
            "Websites" => new Site(file, null, null),
            "Services" => new Service(file, null),
            _ => null
        };

        public string ProcessDescription => Process != null ? $"{Process.ProcessName}.exe" : "";
        public string ProcessIdDescription => Process != null ? $"pid = {Process.Id}" : "";
    }

    public record Site(File Deployment, Process Process, int? Port) : Service(Deployment, Process)
    {
        public string Domain => string.Join('.', Name.Split('.').TakeLast(2));
        public string Subdomain => Name.Split('.').SkipLast(2).FirstOrDefault() ?? MainDomainPart;
        public string FullDomain => Name;
        public string RelevantDomainPart => Subdomain.ToLowerInvariant() == MainDomainPart ? Domain : Subdomain;
        public override Directory Hosting => base.Hosting.Parent / Domain / Subdomain;

        public Uri ProxyTo { get; set; }

        static string MainDomainPart => "main";

        public string PortDescription => Port.HasValue ? $":{Port.Value}" : "";
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

    public async Task Begin()
    {
        Log.Information("watching folder {folder}", root / Deployments);
        await foreach (var files in (root / Deployments).Live(allDirectories: true).Changes())
        {
            if (files.ThereAreChanges)
            {
                Log.Information($"changes found in {string.Join(" & ", files.Added.Concat(files.Changed).Concat(files.Deleted).Select(f => f.Parent.Name).Where(n => Service.Types.Contains(n)).Distinct())}");

                foreach (var file in files.Added.Concat(files.Deleted).Concat(files.Changed).ToArray())
                {
                    Log.Information("stopping {file}", file);
                    await StopDeploySite(file);
                    Log.Information("stopped {file}", file);
                    TrackServicesChanges();
                }
                foreach (var file in files.Added.Concat(files.Changed).ToArray())
                {
                    Log.Information("starting {file}", file);
                    await StartDeploySite(file);
                    TrackServicesChanges();
                }
            }
        }
    }

    public void UseHostingOnApplicationBuilder(IApplicationBuilder app, IHttpForwarder forwarder)
    {
        var transformer = new CustomTransformer(); // or HttpTransformer.Default;
        var requestOptions = new ForwarderRequestConfig { AllowResponseBuffering = false, ActivityTimeout = TimeSpan.FromSeconds(100) };

        var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false
        }, disposeHandler: true);

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/{**catch-all}", async httpContext =>
            {
                if (SiteByDomain(httpContext.Request.Host.Host) is Site site && (site.Port != null || site.ProxyTo != null))
                {
                    var uri = site.Port != null ? $"https://localhost:{site.Port}" : site.ProxyTo.ToString();
                    await forwarder.SendAsync(httpContext, uri, httpClient, requestOptions, transformer);
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    await httpContext.Response.WriteAsync($$"""<html><head><meta http-equiv="refresh" content="1"><style>html{color-scheme:dark;font:2rem monospace;display:grid;height:100%;place-content:center}</style></head><body>404 not found '{{httpContext.Request.Host.Host}}'</body></html>""");
                }
            });
        });

        var _ = Begin();
    }

    Site SiteByDomain(string domain) => services.Values.OfType<Site>().Where(p => p.FullDomain == domain.ToLowerInvariant()).FirstOrDefault();

    async Task StartDeploySite(File file, bool waitForPort = false)
    {
        await Task.Run(async () =>
        {
            if (Service.FromFile(file) is Service service)
            {
                Log.Information($"[{file.Parent.Name.ToUpperInvariant()}] starting \"{file.NameWithoutExtension}\"");

                file.WaitTillReady();

                // todo: implement .txt website handling here

                switch (file.Extension.ToLower())
                {
                    case ".zip":
                        service.Hosting.Parent.Create();
                        service.Deployment.AsZip().ExtractTo(service.Hosting);
                        service = service with { Process = RunHosted(service.Hosting.Filtered("*.exe", allDirectories: false).FirstOrDefault()) };
                        services[file] = service;

                        var portTask = ScanForPort(file);
                        if (waitForPort)
                        {
                            await portTask;
                        }
                        break;

                    case ".txt":
                        var uri = new Uri(await file.ReadText(), UriKind.Absolute);
                        services[file] = (service as Site) with { ProxyTo = uri };
                        break;
                }
            }
        });
    }

    async Task ScanForPort(File file)
    {
        if (services[file] is Site site)
        {
            Log.Information($"[{file.Parent.Name.ToUpperInvariant()}] scanning for Port on PID \"{site.Process.Id}\"");
            Network_Connections.Connection connection = null;
            for (; connection == null; connection = Network_Connections.Connection.Listening.From(site.Process))
            {
                await Task.Delay(500);
            }
            Log.Information($"[{file.Parent.Name.ToUpperInvariant()}] found Port \"{connection.Local.Port}\"");
            services[file] = site with { Port = connection.Local.Port };

            TrackServicesChanges();

            await new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://{site.FullDomain}/"));
        }
    }

    public async Task RestartSite(Site site, bool waitForPort = false)
    {
        Log.Information($"restarting {site.Name}");

        if (services.FirstOrDefault(s => s.Value == site) is { Key: not null } entry)
        {
            var file = entry.Key;

            if (file == null)
            {
                throw new Exception();
            }

            Log.Information("stopping {file}", file);
            await StopDeploySite(file);
            Log.Information("stopped {file}", file);
            TrackServicesChanges();

            Log.Information("starting {file}", file);
            await StartDeploySite(file, waitForPort);
            TrackServicesChanges();
        }

    }

    async Task StopDeploySite(File file)
    {
        try
        {
            Log.Information("stopping ()");
            await Task.Run(() =>
            {
                Log.Information("getting service from file");
                if ((services.GetValueOrDefault(file) ?? Service.FromFile(file)) is Service service)
                {
                    Log.Information("service is {service}", service);
                    if (service.Process is Process process)
                    {
                        Log.Information($"[{file.Parent.Name.ToUpperInvariant()}] stopping \"{file.NameWithoutExtension}\"");

                        process.Kill();
                        process.WaitForExit();
                    }
                    Log.Information("removing service");
                    services.Remove(file);

                    if (service.Hosting.Exists)
                    {
                        Log.Information("deleting service");
                        service.Hosting.Delete();

                        Log.Information("cleaning up folders");
                        if (!service.Hosting.Parent.AllFiles.Any() && service.Hosting.Parent.Parent != root / Live)
                        {
                            service.Hosting.Parent.Delete();
                        }
                    }
                    Log.Information("done");
                }
            });
            Log.Information("stopping () done");
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            Log.Error(e.ToString());
        }
    }

    static Process RunHosted(File file)
    {
        var start = new ProcessStartInfo(file.Path, $"--urls=https://127.0.0.1:0/")
        {
            WorkingDirectory = file.Parent.Path,
            CreateNoWindow = true,
        };

        return FromStackOverflow.ChildProcessTracker.Track(Process.Start(start));
    }
}