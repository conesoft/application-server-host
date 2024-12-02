using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Conesoft.Server_Host.Helpers;
using Serilog;

namespace Conesoft.Server_Host.Features.MediatorService.Services;

class BroadcasterListenerBasedHandler : IHandler, IWaitOneMessage
{
    readonly Dictionary<Type, Queue<ChangeBroadcaster>> broadcasterMap = [];

    void IHandler.Handle<Message>(Message message)
    {
        if (broadcasterMap.GetValueOrDefault(typeof(Message)) is Queue<ChangeBroadcaster> broadcasters)
        {
            foreach (var broadcaster in broadcasters)
            {
                Log.Information("✉ Message {message} heard through a broadcaster", typeof(Message).Name);
                broadcaster.Notify();
            }
            broadcasterMap.Remove(typeof(Message));
        }
    }

    Task IWaitOneMessage.WaitForNextMessage<Message>()
    {
        ChangeBroadcaster broadcaster = new();
        var queue = broadcasterMap.GetValueOrDefault(typeof(Message)) ?? new();
        queue.Enqueue(broadcaster);
        broadcasterMap[typeof(Message)] = queue;
        return broadcaster.WaitForChange();
    }
}