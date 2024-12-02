namespace Conesoft.Server_Host.Features.@Template.Interfaces;

class TemplateService : BackgroundService, IHostedService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}