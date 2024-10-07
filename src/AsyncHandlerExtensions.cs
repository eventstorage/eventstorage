using AsyncHandler.EventSourcing.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing;

public static class AsyncHandlerExtensions
{
    public static IServiceCollection AddAsyncHandler(
        this IServiceCollection services,
        Action<AsyncHandlerConfiguration> configure)
    {
        AsyncHandlerConfiguration asynchandlerConfiguration = new(services);
        configure(asynchandlerConfiguration);
        return services;
    }
    public static EventSourceConfiguration AddEventSourcing(
        this AsyncHandlerConfiguration config,
        Action<EventSourceConfiguration> configure)
    {
        EventSourceConfiguration sourceConfig = new(config.ServiceCollection, config.Schema);
        configure(sourceConfig);
        return sourceConfig;
    }
    public static void EnableTransactionalOutbox(
        this AsyncHandlerConfiguration config,
        MessageBus messageBus,
        string busConnection)
    {
        var eventSourceConfiguration = new EventSourceConfiguration(config.ServiceCollection, "");
        // configure(eventSourceConfiguration);
    }
}
