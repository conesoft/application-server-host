namespace Conesoft.Server_Host.Features.ProxyTarget.Interfaces;

interface ISelectProxyTarget
{
    string? TargetFor(string domain);
}