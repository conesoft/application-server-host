using Conesoft.Server_Host.Features.MediatorService.Interfaces;

namespace Conesoft.Server_Host.Features.MediatorService.Services
{
    class KeyListener<Message>(IServiceProvider services, Func<Message, string> keySelector) : IListener<Message>
    {
        public void Listen(Message message)
        {
            var key = keySelector(message);
            if (services.GetKeyedService<IListener<Message>>(key) is IListener<Message> handler)
            {
                handler.Listen(message);
                return;
            }
            throw new NotSupportedException($"Listening of key '{key}' not supported");
        }
    }
}
