using Conesoft.Server_Host.Features.Mediator.Interfaces;
using Conesoft.Server_Host.Helpers;
using Serilog;

namespace Conesoft.Server_Host.Features.Mediator.Services;

public class MediatorService(IServiceProvider services)
{
    public void Send<Message>(Message message, bool notify = false)
    {
        Log.Information("✉ Sending Message {message}", message);
        if (services.GetService<IHandler<Message>>() is IHandler<Message> handler)
        {
            Log.Information("✉ Message {message} handled by {handler}", typeof(Message).Name, handler.GetType().GetFriendlyName());
            handler.Handle(message);
        }
        if (notify)
        {
            Notify(message);
        }
    }

    public Result Send<Message, Result>(Message message, bool notify = false)
    {
        Log.Information("✉ Sending Message {message}", message);
        try
        {
            if (services.GetService<IHandler<Message>>() is IHandler<Message, Result> handler)
            {
                Log.Information("✉ Message {message} handled by {handler}", typeof(Message).Name, handler.GetType().GetFriendlyName());
                return handler.Handle(message);
            }
            throw new NotImplementedException($"Handler for {typeof(Message).Name} not implemented");
        }
        finally
        {
            if (notify)
            {
                Notify(message);
            }
        }
    }

    public void Notify<Message>(Message message)
    {
        Log.Information("✉ Sending Notification Message {message}", message);
        foreach (var listener in services.GetServices<IListener<Message>>())
        {
            Log.Information("✉ Message {message} heard by {listener}", typeof(Message).Name, listener.GetType().Name);
            listener.Listen(message);
        }
    }
}
