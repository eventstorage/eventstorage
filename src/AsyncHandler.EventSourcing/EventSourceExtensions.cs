using System.Reflection;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Extensions;
using AsyncHandler.EventSourcing.Projections;
using AsyncHandler.EventSourcing.Repositories;
using AsyncHandler.EventSourcing.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AsyncHandler.EventSourcing;

public static class EventSourceExtensions
{
    public static EventSourceConfiguration SelectEventSource(
        this EventSourceConfiguration configuration,
        EventSources source,
        string connectionString)
    {
        Type? aggregateType = typeof(AggregateRoot).GetClientAggregate(Assembly.GetCallingAssembly());
        if (aggregateType == null)
            return configuration;
        
        configuration.ServiceCollection.AddSingleton<IHostedService>((sp) =>
        {
            return new SourceInitializer(new Repository<AggregateRoot>(connectionString, sp), source);
        });
        #pragma warning disable CS8603
        Type repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(aggregateType);
        Type repositoryType = typeof(Repository<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddTransient(repositoryInterfaceType, sp =>
        {
            return Activator.CreateInstance(repositoryType, connectionString, sp);
        });
        Type eventSourceInterfaceType = typeof(IEventSource<>).MakeGenericType(aggregateType);
        Type eventSourceType = typeof(EventSource<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddTransient(eventSourceInterfaceType, sp =>
        {
            return Activator.CreateInstance(eventSourceType, sp.GetRequiredService<IRepository<AggregateRoot>>(), source);
        });
        return configuration;
    }
    public static EventSourceConfiguration AddProjection<T>(
        this EventSourceConfiguration configuration,
        ProjectionMode projectionMode)
    {
        return configuration;
    }
}
