using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Serilog;

namespace Conesoft.Server_Host.Features.MediatorService.Services;

class DependencyInjectionBasedHandler(IServiceProvider services) : IHandler
{
    void IHandler.Handle<Message>(Message message)
    {
        foreach (var listener in services.GetServices<IListener<Message>>())
        {
            Log.Information("✉ Message {message} heard by {listener}", typeof(Message).Name, listener.GetType().Name);
            listener.Listen(message);
        }
    }
}
