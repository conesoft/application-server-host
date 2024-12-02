namespace Conesoft.Server_Host.Features.MediatorService.Interfaces;

public interface IWaitOneMessage
{
    Task WaitForNextMessage<Message>();
}
