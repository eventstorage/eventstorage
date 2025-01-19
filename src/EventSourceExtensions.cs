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
    public static EventSourceConfiguration Select(
        this EventSourceConfiguration configuration, EventStore store, string? connectionString = null)
    {
        IEventStorageSchema clientSchema = store switch
        {
            EventStore.PostgresSql => new PostgreSqlSchema(configuration.Schema),
            EventStore.AzureSql => new AzureSqlSchema(configuration.Schema),
            _ => new SqlServerSchema(configuration.Schema),
        };
        configuration.ServiceCollection.AddSingleton(typeof(IEventStorageSchema), sp => clientSchema);

        var client = store switch
        {
            EventStore.PostgresSql => typeof(PostgreSqlClient<>).MakeGenericType(configuration.T),
            _ => typeof(SqlServerClient<>).MakeGenericType(configuration.T)
        };
        var eventStorage = typeof(IEventStorage<>).MakeGenericType(configuration.T);
        configuration.ServiceCollection.AddScoped(eventStorage, sp =>
            Activator.CreateInstance(client, sp, connectionString?? configuration.ConnectionString)?? default!
        );

        configuration.ConnectionString = connectionString?? configuration.ConnectionString;
        configuration.Store = store;
        return configuration;
    }
    public static EventSourceConfiguration Project<TProjection>(
        this EventSourceConfiguration configuration,
        ProjectionMode mode = ProjectionMode.Async,
        Func<ProjectionConfiguration, ProjectionConfiguration> source = default!)
        where TProjection : Projection, new()
    {
        if(source != null && mode != ProjectionMode.Async)
            throw new Exception($"Projection to source only allowed with async mode.");
        
        source ??= (config) => new();
        var projection = new TProjection { Mode = mode, Configuration = source(new())};
        var mprojection = typeof(TProjection).GetInterfaces().Last();
        configuration.ServiceCollection.AddSingleton(mprojection, projection);
        configuration.ServiceCollection.AddSingleton(typeof(IProjection), projection);
        configuration.Projections.Add(projection);
        return configuration;
    }
}
