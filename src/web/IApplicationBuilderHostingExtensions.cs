using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Host.Web
{
    public static class IApplicationBuilderHostingExtensions
    {
        public static IApplicationBuilder UseHosting(this IApplicationBuilder app, string responseUrl, IHttpForwarder forwarder)
        {
            var hosting = app.ApplicationServices.GetService<Hosting>();

            app.UseRouting();

            hosting.UseHostingOnApplicationBuilder(app, responseUrl, forwarder);

            return app;
        }
    }
}
