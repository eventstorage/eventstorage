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
        Type? aggregateType = typeof(AggregateRoot).GetAggregate(Assembly.GetCallingAssembly());
        if (aggregateType == null)
            return configuration;
        
        // initialize source when app spins up
        configuration.ServiceCollection.AddSingleton<IHostedService>((sp) =>
        {
            var repository = new Repository<AggregateRoot>(connectionString, sp);
            return new SourceInitializer(new EventSource<AggregateRoot>(repository, source));
        });
        #pragma warning disable CS8603
        // register repository
        var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(aggregateType);
        var repositoryType = typeof(Repository<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddScoped(repositoryInterfaceType, sp =>
        {
            return Activator.CreateInstance(repositoryType, connectionString, sp);
        });
        // register event source
        Type eventSourceInterfaceType = typeof(IEventSource<>).MakeGenericType(aggregateType);
        Type eventSourceType = typeof(EventSource<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddScoped(eventSourceInterfaceType, sp =>
        {
            var repository = sp.GetService(repositoryInterfaceType);
            return Activator.CreateInstance(eventSourceType, repository, source);
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
