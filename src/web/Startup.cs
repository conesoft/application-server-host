using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Server_Host.Web;

public class Startup
{
    readonly Uri httpUrl;
    readonly Uri httpsUrl;

    public Startup(IConfiguration configuration)
    {
        httpUrl = new("http://localhost");
        httpsUrl = new("https://localhost");

        var webHostDefaults = configuration[WebHostDefaults.ServerUrlsKey];
        if(webHostDefaults != null)
        {
            httpUrl = new(webHostDefaults.Split(';').FirstOrDefault(u => u.StartsWith("http://")) ?? httpUrl.ToString());
            httpsUrl = new(webHostDefaults.Split(';').FirstOrDefault(u => u.StartsWith("https://")) ?? httpsUrl.ToString());
        }
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHosting();

        services.AddHsts(config =>
        {
            config.IncludeSubDomains = true;
            config.MaxAge = System.TimeSpan.FromDays(365);
            config.Preload = true;
        });
        services.AddHttpsRedirection(config =>
        {
            config.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            config.HttpsPort = httpsUrl.Port;
        });
    }

    public void Configure(IApplicationBuilder app, IHttpForwarder forwarder)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
        
        app.UseHosting(forwarder);
    }
}