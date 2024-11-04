using Conesoft.Server_Host.Features.ProxyTarget.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Server_Host.Features.ProxyTarget.Extensions;

static class ProxyTargetExtensions
{
    public static WebApplicationBuilder AddProxyTargetSelection<T>(this WebApplicationBuilder builder) where T : class, ISelectProxyTarget
    {
        builder.Services.AddSingleton<ISelectProxyTarget, T>();
        builder.Services.AddHttpForwarder();

        return builder;
    }

    public static IApplicationBuilder MapProxyTargets(this IApplicationBuilder app)
    {
        var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
        {
            // based on https://microsoft.github.io/reverse-proxy/articles/direct-forwarding.html
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15),
            ConnectCallback = async (context, cancellationToken) =>
            {
                // based on https://www.meziantou.net/forcing-httpclient-to-use-ipv4-or-ipv6-addresses.htm
                var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork, cancellationToken);
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                try
                {
                    await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        }, disposeHandler: false);
        var forwarder = app.ApplicationServices.GetRequiredService<IHttpForwarder>();

        return app.Use(async (context, next) =>
        {
            if (context.RequestServices.GetRequiredService<ISelectProxyTarget>().TargetFor(context.Request.Host.Host) is string target)
            {
                await forwarder.SendAsync(context, target, httpClient);
            }
            else
            {
                await next(context);
            }
        });
    }
}