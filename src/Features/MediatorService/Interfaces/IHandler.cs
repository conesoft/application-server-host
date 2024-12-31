namespace Conesoft.Server_Host.Features.MediatorService.Interfaces;

public interface IHandler
{
    void Handle<Message>(Message message);
}
