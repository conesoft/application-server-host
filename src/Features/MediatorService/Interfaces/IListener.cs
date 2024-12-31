namespace Conesoft.Server_Host.Features.MediatorService.Interfaces
{
    public interface IListener { }

    public interface IListener<Message> : IListener
    {
        void Listen(Message message);
    }
}
