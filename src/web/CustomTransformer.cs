using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Host.Web
{
    class CustomTransformer : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
            proxyRequest.Headers.Host = null;
        }
    }
}
