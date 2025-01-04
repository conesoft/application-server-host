using Conesoft.Server_Host.Features.MediatorService.Interfaces;
using Conesoft.Server_Host.Features.MediatorService.Services;

namespace Conesoft.Server_Host.Features.MediatorService.Extensions;

static class AddMediatorExtensions
{
    public static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHandler, DependencyInjectionBasedHandler>();
        builder.Services.AddSingleton<BroadcasterListenerBasedHandler>();
        builder.Services.AddTransient<IHandler>(s => s.GetRequiredService<BroadcasterListenerBasedHandler>());
        builder.Services.AddTransient<IWaitOneMessage>(s => s.GetRequiredService<BroadcasterListenerBasedHandler>());
        builder.Services.AddSingleton<Mediator>();
        return builder;
    }

    public static IServiceCollection AddMediatingSingleton<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<T>();
        foreach (var i in typeof(T).GetInterfaces())
        {
            if (i.IsAssignableTo(typeof(IListener)) && i != typeof(IListener))
            {
                services.AddTransient(i, s => s.GetRequiredService<T>());
            }
        }
        return services;
    }

    public static IServiceCollection AddMediatingHostedService<T>(this IServiceCollection services) where T : class, IHostedService
    {
        services.AddSingleton<T>();
        services.AddHostedService(s => s.GetRequiredService<T>());
        foreach (var i in typeof(T).GetInterfaces())
        {
            if (i.IsAssignableTo(typeof(IListener)) && i != typeof(IListener))
            {
                services.AddTransient(i, s => s.GetRequiredService<T>());
            }
        }
        return services;
    }

    public static IServiceCollection AddKeyedMediatingSingleton<T>(this IServiceCollection services, object? key) where T : class
    {
        services.AddSingleton<T>();
        foreach (var i in typeof(T).GetInterfaces())
        {
            if (i.IsAssignableTo(typeof(IListener)) && i != typeof(IListener))
            {
                services.AddKeyedTransient(i, key, (s, k) => s.GetRequiredService<T>());
            }
        }

        return services;
    }

    public static IServiceCollection AddKeyedMessage<T>(this IServiceCollection services, Func<T, string> keySelector) where T : class
    {
        return services.AddSingleton(s => new KeyListener<T>(s, keySelector) as IListener<T>);
    }
}