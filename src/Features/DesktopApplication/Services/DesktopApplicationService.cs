namespace Conesoft.Server_Host.Features.DesktopApplication.Services;

class DesktopApplicationService(IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services) : IHostedService
{
    MainApplication? app = default;
    private readonly Serilog.ILogger log = Serilog.Log.ForContext<DesktopApplicationService>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var thread = new Thread(() =>
        {
            log.Information("starting desktop application service");
            services.GetRequiredService<ThemeService>(); // instanciating in the STA Thread
            app = services.GetRequiredService<MainApplication>();
            app.InitializeComponent();
            app.Run(services.GetRequiredService<MainWindow>());
            log.Information("main application window closed");
            app = null;
            hostApplicationLifetime.StopApplication();
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        log.Information("stopping desktop application service");
        try
        {
            if (app != null)
            {
                await app.Dispatcher.InvokeAsync(app.Shutdown);
            }
        }
        catch (Exception)
        {
            /* try catch in case of a race condition */
        }
    }
}