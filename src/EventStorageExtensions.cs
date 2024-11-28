using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage;

public static class EventStorageExtensions
{
    public static IServiceCollection AddEventStorage(
        this IServiceCollection services,
        Action<EventStorageConfiguration> configure)
    {
        EventStorageConfiguration config = new EventSourceConfiguration<IEventSource>(services);
        configure(config);
        return services;
    }
    public static EventStorageConfiguration AddEventSource(
        this EventStorageConfiguration config,
        Action<EventSourceConfiguration<IEventSource>> configure)
    {
        EventSourceConfiguration<IEventSource> eventsource = new(
            config.ServiceCollection,
            config.Schema,
            config.ConnectionString);
        
        configure(eventsource);
        return eventsource.Initialize()
            .ConfigureProjectionRestorer()
            .RunAsyncProjectionEngine();
    }
    public static EventSourceConfiguration<T> AddEventSource<T>(
        this EventStorageConfiguration config,
        Action<EventSourceConfiguration<T>> configure) where T : IEventSource
    {
        EventSourceConfiguration<T> eventsource = new(
            config.ServiceCollection,
            config.Schema,
            config.ConnectionString);
        
        configure(eventsource);
        return eventsource.Initialize()
            .ConfigureProjectionRestorer()
            .RunAsyncProjectionEngine();
    }
    public static void EnableTransactionalOutbox(
        this EventStorageConfiguration config,
        MessageBus messageBus,
        string busConnection)
    {
        EventSourceConfiguration<IEventSource> eventSourceConfiguration = new(config.ServiceCollection);
        // configure(eventSourceConfiguration);
    }
}
