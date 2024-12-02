using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Serilog;

namespace Conesoft.Server_Host.Features.MediatorService.Services;

public class Mediator(IEnumerable<IHandler> handlers)
{
    public void Notify<Message>(Message message)
    {
        Log.Information("✉ Sending Notification Message {message}", message);
        foreach (var handler in handlers)
        {
            handler.Handle(message);
        }
    }
}