namespace Conesoft.Server_Host.Features.Mediator.Interfaces
{
    public interface IListener { }

    public interface IListener<Message> : IListener
    {
        void Listen(Message message);
    }
}
