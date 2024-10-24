using EventStorage.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage;

public static class EventStorageExtensions
{
    public static IServiceCollection AddEventStorage(
        this IServiceCollection services,
        Action<EventStorageConfiguration> configure)
    {
        EventStorageConfiguration eventStorageConfiguration = new(services);
        configure(eventStorageConfiguration);
        return services;
    }
    public static EventSourceConfiguration AddEventSource(
        this EventStorageConfiguration config,
        Action<EventSourceConfiguration> configure)
    {
        EventSourceConfiguration eventsource = new(config.ServiceCollection, config.Schema);
        configure(eventsource);
        eventsource.InitStore();
        return eventsource;
    }
    public static void EnableTransactionalOutbox(
        this EventStorageConfiguration config,
        MessageBus messageBus,
        string busConnection)
    {
        var eventSourceConfiguration = new EventSourceConfiguration(config.ServiceCollection, "");
        // configure(eventSourceConfiguration);
    }
}
