using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Service.Proxy;

namespace Conesoft.Host.Web
{
    public static class IApplicationBuilderHostingExtensions
    {
        public static IApplicationBuilder UseHosting(this IApplicationBuilder app, string responseUrl, IHttpProxy proxy)
        {
            var hosting = app.ApplicationServices.GetService<Hosting>();

            app.UseRouting();

            hosting.UseHostingOnApplicationBuilder(app, responseUrl, proxy);

            return app;
        }
    }
}
