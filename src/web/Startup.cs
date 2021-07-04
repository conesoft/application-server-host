using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Host.Web
{
    public class Startup
    {
        readonly string httpUrl;

        public Startup(IConfiguration configuration)
        {
            httpUrl = configuration[WebHostDefaults.ServerUrlsKey].Split(';').FirstOrDefault(u => u.StartsWith("http://"));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHosting();
        }

        public void Configure(IApplicationBuilder app, IHttpForwarder forwarder)
        {
            app.UseHosting(httpUrl, forwarder);
        }
    }
}