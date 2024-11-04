namespace Conesoft.Server_Host.Features.Mediator.Interfaces
{
    public interface IHandler { }
    public interface IHandler<Message> : IHandler
    {
        void Handle(Message message);
    }
    public interface IHandler<Message, Result> : IHandler
    {
        Result Handle(Message message);
    }

    class KeyHandler<T>(IServiceProvider services, Func<T, string> keySelector) : IHandler<T>
    {
        public void Handle(T message)
        {
            var key = keySelector(message);
            if (services.GetKeyedService<IHandler<T>>(key) is IHandler<T> handler)
            {
                handler.Handle(message);
                return;
            }
            throw new NotSupportedException($"Handling of key '{key}' not supported");
        }
    }
}