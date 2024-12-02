using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;
using TDiscover;

namespace EventStorage;

public static class EventStorageExtensions
{
    public static IServiceCollection AddEventStorage(
        this IServiceCollection services,
        Action<EventStorageConfiguration> configure)
    {
        EventStorageConfiguration config = new EventSourceConfiguration(services);
        configure(config);
        return services;
    }
    public static EventSourceConfiguration AddEventSource(
        this EventStorageConfiguration config,
        Action<EventSourceConfiguration> configure)
    {
        Type aggregateType = Td.FindByCallingAsse<IEventSource>(Assembly.GetCallingAssembly())??
            throw new Exception("No aggregate found.");
        EventSourceConfiguration source = new(config.ServiceCollection, config.Schema, config.ConnectionString);
        source.T = aggregateType;

        configure(source);
        return source.Initialize()
            .ConfigureProjectionRestorer()
            .RunAsyncProjectionEngine();
    }
    public static void Dispatch(
        this EventStorageConfiguration config,
        MessageBus messageBus,
        string busConnection)
    {
        EventSourceConfiguration eventSourceConfiguration = new(config.ServiceCollection);
        // configure(eventSourceConfiguration);
    }
}
