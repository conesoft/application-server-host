using Conesoft.Server_Host.Features.Mediator.Interfaces;
using Conesoft.Server_Host.Features.Mediator.Services;

namespace Conesoft.Server_Host.Features.Mediator.Extensions;

static class AddMediatorExtensions
{
    public static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<MediatorService>();
        return builder;
    }

    public static IServiceCollection AddMediatingSingleton<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton<T>();
        foreach (var i in typeof(T).GetInterfaces())
        {
            if (i.IsAssignableTo(typeof(IHandler)) && i != typeof(IHandler))
            {
                services.AddTransient(i, s => s.GetRequiredService<T>());
            }
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
            if (i.IsAssignableTo(typeof(IHandler)) && i != typeof(IHandler))
            {
                services.AddKeyedTransient(i, key, (s, k) => s.GetRequiredService<T>());
            }
            if (i.IsAssignableTo(typeof(IListener)) && i != typeof(IListener))
            {
                services.AddKeyedTransient(i, key, (s, k) => s.GetRequiredService<T>());
            }
        }

        return services;
    }

    public static IServiceCollection AddKeyedMessage<T>(this IServiceCollection services, Func<T, string> keySelector) where T : class
    {
        return services.AddSingleton(s => new KeyHandler<T>(s, keySelector) as IHandler<T>);
    }
}