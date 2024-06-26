﻿using Conesoft.Files;
using Conesoft.Network_Connections;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Server_Host.Web;

public class Hosting(IConfiguration configuration)
{
    public record Service(File Deployment, Process? Process)
    {
        public string Name => Deployment.NameWithoutExtension.ToLowerInvariant();
        public virtual Directory Hosting => Deployment.Parent.Parent.Parent / Live / Deployment.Parent.Name / Deployment.NameWithoutExtension;



        public static string[] Types => ["Services", "Websites"];
        public static Service? FromFile(File file) => file.Parent.Name switch
        {
            "Websites" => new Site(file, null, null),
            "Services" => new Service(file, null),
            _ => null
        };

        public string ProcessDescription => Process != null ? $"{Process.ProcessName}.exe" : "";
        public string ProcessIdDescription => Process != null ? $"pid = {Process.Id}" : "";
    }

    public record Site(File Deployment, Process? Process, int? Port) : Service(Deployment, Process)
    {
        public string Domain => string.Join('.', Name.Split('.').TakeLast(2));
        public string Subdomain => Name.Split('.').SkipLast(2).FirstOrDefault() ?? MainDomainPart;
        public string FullDomain => Name;
        public string RelevantDomainPart => Subdomain.ToLowerInvariant() == MainDomainPart ? Domain : Subdomain;
        public override Directory Hosting => base.Hosting.Parent / Domain / Subdomain;

        public Uri? ProxyTo { get; set; }

        static string MainDomainPart => "main";

        public string PortDescription => Port.HasValue ? $":{Port.Value}" : "";
    }

    readonly Directory root = Directory.From(configuration["hosting:root"]!);
    readonly Dictionary<File, Service> services = [];

    public event Action<Service[]>? OnServicesChanged;
    public void TrackServicesChanges() => OnServicesChanged?.Invoke([.. services.Values]);

    static string Deployments => "Deployments";
    static string Live => "Live";

    public async Task Begin()
    {
        Log.Information("watching folder {folder}", root / Deployments);
        await foreach (var entries in (root / Deployments).Live(allDirectories: true).Changes())
        {
            if (entries.ThereAreChanges)
            {
                var added = entries.Added?.Files() ?? [];
                var changed = entries.Changed?.Files() ?? [];
                var deleted = entries.Deleted?.Files() ?? [];
                Log.Information($"changes found in {string.Join(" & ", added.Concat(changed).Concat(deleted).Select(f => f.Parent.Name).Where(n => Service.Types.Contains(n)).Distinct())}");

                foreach (var file in added.Concat(deleted).Concat(changed).ToArray())
                {
                    Log.Information("stopping {file}", file);
                    await StopDeploySite(file);
                    Log.Information("stopped {file}", file);
                    TrackServicesChanges();
                }
                foreach (var file in added.Concat(changed).ToArray())
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

        var socketHttpHandler = new SocketsHttpHandler()
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,

            PooledConnectionIdleTimeout = TimeSpan.MaxValue, //<<< potentially fixes the 2s delays
            ///PooledConnectionLifetime = TimeSpan.MaxValue,
            //ConnectTimeout = TimeSpan.Zero,
            Expect100ContinueTimeout = TimeSpan.Zero,
            //KeepAlivePingTimeout = TimeSpan.Zero,
            ///ResponseDrainTimeout = TimeSpan.Zero,
            KeepAlivePingDelay = TimeSpan.FromSeconds(1)
        };
        var httpClient = new HttpMessageInvoker(socketHttpHandler, disposeHandler: true);

        var timer = new Stopwatch();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/{**catch-all}", async httpContext =>
            {
                if (SiteByDomain(httpContext.Request.Host.Host) is Site site && (site.Port != null || site.ProxyTo != null))
                {
                    var uri = site.Port != null ? $"https://localhost:{site.Port}" : site.ProxyTo!.ToString();
                    
                    timer.Restart();
                    var error = await forwarder.SendAsync(httpContext, uri, httpClient, requestOptions, transformer);
                    Log.Information($"request to {httpContext.Request.GetDisplayUrl()} by {httpContext.Connection.RemoteIpAddress} resolved in {timer.Elapsed.Humanize(precision: 2)} with error {error}");
                    timer.Stop();
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    await httpContext.Response.WriteAsync($$"""<html><head><meta http-equiv="refresh" content="1"><style>html{background:black;color-scheme:dark;font:2rem monospace;display:grid;height:100%;place-content:center}a{color:mediumspringgreen}</style></head><body>404 not found <a href='https://{{httpContext.Request.Host.Host}}'>{{httpContext.Request.Host.Host}}</a></body></html>""");
                }
            });
        });

        var _ = Begin();
    }

    Site? SiteByDomain(string domain) => services.Values.OfType<Site>().Where(p => p.FullDomain == domain.ToLowerInvariant()).FirstOrDefault() ?? null;

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
                        if (service.Hosting.FilteredFiles("*.exe", allDirectories: false).FirstOrDefault() is File executable)
                        {
                            service = service with { Process = RunHosted(executable) };
                            services[file] = service;

                            var portTask = ScanForPort(file);
                            if (waitForPort)
                            {
                                await portTask;
                            }
                        }
                        break;

                    case ".txt":
                        if ((await file.ReadText()) is string url && service is Site site)
                        {
                            var uri = new Uri(url, UriKind.Absolute);
                            services[file] = site with { ProxyTo = uri };
                        }
                        break;
                }
            }
        });
    }

    static Task<bool> IsHttpsPort(ushort port, CancellationToken ct) => new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, $"https://localhost:{port}/"), ct).ContinueWith(t => t.IsCompletedSuccessfully);

    static async Task<Connection?> FindHttpsPortOnProcess(Process process, CancellationToken ct = default)
    {
        while (ct.IsCancellationRequested == false)
        {
            var connections = process.GetListeningPorts();
            var connection = await connections.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(async c => await IsHttpsPort(c.Local.Port, ct), ct);
            if (connection != null)
            {
                return connection;
            }
            await Task.Delay(200, ct);
        }
        return null;
    }

    async Task ScanForPort(File file)
    {
        if (services[file] is Site site && site.Process is Process process)
        {
            Log.Information($"[{file.Parent.Name.ToUpperInvariant()}] scanning for Port on PID \"{process.Id}\"");
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var connection = await FindHttpsPortOnProcess(process, cts.Token);
            if (connection != null)
            {
                var port = connection.Local.Port;
                Log.Information($"[{file.Parent.Name.ToUpperInvariant()}] found Port \"{port}\"");
                services[file] = site with { Port = port };

                TrackServicesChanges();

                // head is not enough to get site warmed up, need to read response
                var response = await new HttpClient().GetAsync($"https://{site.FullDomain}/");
                await response.Content.ReadAsStringAsync();
            }
        }
    }

    public async Task RestartSite(Site site, bool waitForPort = false)
    {
        Log.Information($"restarting {site.Name}");

        if (services.FirstOrDefault(s => s.Value == site) is { Key: not null } entry)
        {
            var file = entry.Key ?? throw new Exception();

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

    static Process? RunHosted(File file)
    {
        var start = new ProcessStartInfo(file.Path, $"--urls=https://127.0.0.1:0/")
        {
            WorkingDirectory = file.Parent.Path,
            CreateNoWindow = true,
        };

        return FromStackOverflow.ChildProcessTracker.Track(Process.Start(start));
    }
}