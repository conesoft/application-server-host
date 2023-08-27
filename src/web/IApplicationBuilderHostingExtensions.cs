using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Server_Host.Web;

public static class IApplicationBuilderHostingExtensions
{
    public static IApplicationBuilder UseHosting(this IApplicationBuilder app, IHttpForwarder forwarder)
    {
        var hosting = app.ApplicationServices.GetRequiredService<Hosting>();

        app.UseRouting();

        hosting.UseHostingOnApplicationBuilder(app, forwarder);

        return app;
    }
}