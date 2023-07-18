using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Conesoft.Host.Web
{
    class CustomTransformer : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
            proxyRequest.Headers.Host = null;
        }
    }
}
