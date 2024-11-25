using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Schema;
using Microsoft.Extensions.DependencyInjection;
using TDiscover;

namespace EventStorage;

public static class EventSourceExtensions
{
    public static EventSourceConfiguration Select(
        this EventSourceConfiguration configuration, EventStore source, string connectionString)
    {
        Type? aggregateType = Td.FindByCallingAsse<IEventSource>(Assembly.GetCallingAssembly());
        if (aggregateType == null)
            return configuration;
        configuration.ServiceCollection.AddSchema(configuration.Schema);
        
        // #pragma warning disable CS8603
        // // register repository
        // var repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(aggregateType);
        // var repositoryType = typeof(Repository<>).MakeGenericType(aggregateType);
        // configuration.ServiceCollection.AddScoped(repositoryInterfaceType, sp =>
        // {
        //     return Activator.CreateInstance(repositoryType, connectionString, sp, source);
        // });
        // register event storage
        Type eventSourceInterfaceType = typeof(IEventStorage<>).MakeGenericType(aggregateType);
        Type eventSourceType = typeof(EventStorage<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddScoped(eventSourceInterfaceType, sp =>
        {
            var repository = sp.GetService(repositoryInterfaceType);
            return Activator.CreateInstance(eventSourceType, repository, source);
        });
        configuration.ConnectionString = connectionString;
        configuration.Source = source;
        return configuration;
    }
    private static IServiceCollection AddSchema(this IServiceCollection services, string schema)
    {
        Dictionary<EventStore, IEventSourceSchema> schemas = [];
        schemas.Add(EventStore.AzureSql, new AzureSqlSchema(schema));
        schemas.Add(EventStore.PostgresSql, new PostgreSqlSchema(schema));
        schemas.Add(EventStore.SqlServer, new SqlServerSchema(schema));
        services.AddKeyedSingleton("Schema", schemas);
        return services;
    }
    public static EventSourceConfiguration Project<TProjection>(
        this EventSourceConfiguration configuration,
        ProjectionMode mode = ProjectionMode.Async,
        Func<ProjectionConfiguration, ProjectionConfiguration> source = default!)
        where TProjection : Projection, new()
    {
        if(source != null && mode != ProjectionMode.Async)
            throw new Exception($"Projection to source only allowed with async mode.");
        var iprojection = typeof(TProjection).GetInterfaces().Last();
        source ??= (config) => new();
        var tprojection = new TProjection { Mode = mode, Configuration = source(new())};
        configuration.ServiceCollection.AddSingleton(iprojection, tprojection);
        configuration.ServiceCollection.AddSingleton(typeof(IProjection), tprojection);
        configuration.Projections.Add(tprojection);
        return configuration;
    }
}
