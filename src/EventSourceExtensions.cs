using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Infrastructure;
using EventStorage.Projections;
using EventStorage.Schema;
using Microsoft.Extensions.DependencyInjection;
using TDiscover;

namespace EventStorage;

public static class EventSourceExtensions
{
    public static EventStorageConfiguration Select(
        this EventStorageConfiguration configuration, EventStore store, string? connectionString = null)
    {
        Type aggregateType = configuration.GetType().GenericTypeArguments[0];

        configuration.ConnectionString ??= connectionString;
        configuration.Store = store;
        
        var clientSchema = store switch
        {
            EventStore.PostgresSql => typeof(PostgreSqlSchema<>).MakeGenericType(aggregateType),
            EventStore.AzureSql => typeof(AzureSqlSchema<>).MakeGenericType(aggregateType),
            _ => typeof(SqlServerSchema<>).MakeGenericType(aggregateType)
        };
        var storageSchema = typeof(IEventStorageSchema<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddSingleton(storageSchema, sp =>
            Activator.CreateInstance(clientSchema, configuration.Schema)?? default!
        );

        var client = store switch
        {
            EventStore.PostgresSql => typeof(PostgreSqlClient<>).MakeGenericType(aggregateType),
            _ => typeof(SqlServerClient<>).MakeGenericType(aggregateType)
        };
        var eventStorage = typeof(IEventStorage<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddScoped(eventStorage, sp =>
            Activator.CreateInstance(client, sp, configuration.ConnectionString)?? default!
        );
        return configuration;
    }
    public static EventStorageConfiguration Project<TProjection>(
        this EventStorageConfiguration configuration,
        ProjectionMode mode = ProjectionMode.Async,
        Func<ProjectionConfiguration, ProjectionConfiguration> source = default!)
        where TProjection : Projection, new()
    {
        if(source != null && mode != ProjectionMode.Async)
            throw new Exception($"Projection to source only allowed with async mode.");
        
        Type aggregateType = configuration.GetType().GenericTypeArguments[0];
        
        source ??= (config) => new();
        var projection = new TProjection { Mode = mode, Configuration = source(new())};
        var mprojection = typeof(TProjection).GetInterfaces().First();
        configuration.ServiceCollection.AddSingleton(mprojection, projection);
        var tprojection = typeof(IProjection<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddSingleton(tprojection, projection);
        configuration.Projections.Add(projection);
        
        var pt = configuration.Sp.GetService(tprojection);
        var ptm = configuration.Sp.GetService(mprojection);
        return configuration;
    }
}
