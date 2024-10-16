using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Schema;
using EventStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TDiscover;

namespace EventStorage;

public static class EventSourceExtensions
{
    public static EventSourceConfiguration SelectEventStorage(
        this EventSourceConfiguration configuration,
        EventStore source,
        string connectionString)
    {
        Type? aggregateType = Td.FindByCallingAsse<IEventSource>(Assembly.GetCallingAssembly());
        if (aggregateType == null)
            return configuration;
        configuration.ServiceCollection.AddEventSourceSchema(configuration.Schema);
        // initialize source while app spins up
        configuration.ServiceCollection.AddSingleton<IHostedService>((sp) =>
        {
            var repository = new Repository<IEventSource>(connectionString, sp, source);
            return new SourceInitializer(new EventStorage<IEventSource>(repository, source));
        });
        #pragma warning disable CS8603
        // register repository
        var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(aggregateType);
        var repositoryType = typeof(Repository<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddScoped(repositoryInterfaceType, sp =>
        {
            return Activator.CreateInstance(repositoryType, connectionString, sp, source);
        });
        // register event storage
        Type eventSourceInterfaceType = typeof(IEventStorage<>).MakeGenericType(aggregateType);
        Type eventSourceType = typeof(EventStorage<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddScoped(eventSourceInterfaceType, sp =>
        {
            var repository = sp.GetService(repositoryInterfaceType);
            return Activator.CreateInstance(eventSourceType, repository, source);
        });
        return configuration;
    }
    public static EventSourceConfiguration Project<TProjection>(
        this EventSourceConfiguration configuration,
        ProjectionMode mode) where TProjection : IProjection, new()
    {
        var iprojection = typeof(TProjection).GetInterfaces().First();
        configuration.ServiceCollection.AddSingleton(iprojection, sp =>
        {
            dynamic tprojection = Activator.CreateInstance<TProjection>();
            tprojection.Mode = mode;
            return tprojection;
        });
        configuration.ServiceCollection.AddSingleton<IProjectionEngine,ProjectionEngine>();
        return configuration;
    }
    private static IServiceCollection AddEventSourceSchema(this IServiceCollection services, string schema)
    {
        Dictionary<EventStore, IEventSourceSchema> schemas = [];
        schemas.Add(EventStore.AzureSql, new AzureSqlSchema(schema));
        schemas.Add(EventStore.PostgresSql, new PostgreSqlSchema(schema));
        schemas.Add(EventStore.SqlServer, new SqlServerSchema(schema));
        services.AddKeyedSingleton("Schema", schemas);
        return services;
    }
}
