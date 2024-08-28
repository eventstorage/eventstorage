using AsyncHandler.EventSourcing.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing;

public static class AsyncHandlerExtensions
{
    public static IServiceCollection AddAsyncHandler(
        this IServiceCollection services,
        Action<AsyncHandlerConfiguration> configure)
    {
        var asynchandlerConfiguration = new AsyncHandlerConfiguration(services);
        configure(asynchandlerConfiguration);
        return services;
    }
    public static EventSourceConfiguration AddEventSourcing(
        this AsyncHandlerConfiguration configuration,
        Action<EventSourceConfiguration> configure)
    {
        var eventSourceConfiguration = new EventSourceConfiguration(configuration.ServiceCollection);
        configure(eventSourceConfiguration);
        return eventSourceConfiguration;
    }
    public static void EnableTransactionalOutbox(
        this AsyncHandlerConfiguration configuration,
        MessageBus messageBus,
        string busConnection)
    {
        var eventSourceConfiguration = new EventSourceConfiguration(configuration.ServiceCollection);
        // configure(eventSourceConfiguration);
    }
}
