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
        this EventStorageConfiguration configuration,
        EventStore store,
        string? connectionString = null)
    {
        Type? aggregateType = null;
        if(configuration.GetType().GenericTypeArguments[0].Name != "IEventSource")
            aggregateType = configuration.GetType().GenericTypeArguments[0];
        aggregateType ??= Td.FindByCallingAsse<IEventSource>(Assembly.GetCallingAssembly());
        ArgumentNullException.ThrowIfNull(aggregateType);
        
        EventStorageSchema clientSchema = store switch
        {
            EventStore.PostgresSql => new PostgreSqlSchema(configuration.Schema),
            EventStore.AzureSql => new AzureSqlSchema(configuration.Schema),
            _ => new SqlServerSchema(configuration.Schema)
        };
        configuration.ServiceCollection.AddSingleton(typeof(IEventStorageSchema), clientSchema);

        var eventStorageType = typeof(IEventStorage<>).MakeGenericType(aggregateType);
        var postgresClientType = typeof(PostgreSqlClient<>).MakeGenericType(aggregateType);
        var mssqlClientType = typeof(SqlServerClient<>).MakeGenericType(aggregateType);
        var client = store switch
        {
            EventStore.PostgresSql => postgresClientType,
            _ => mssqlClientType
        };
        configuration.ConnectionString ??= connectionString;
        configuration.Store = store;
        
        configuration.ServiceCollection.AddScoped(eventStorageType, sp =>
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
        var iprojection = typeof(TProjection).GetInterfaces().Last();
        source ??= (config) => new();
        var tprojection = new TProjection { Mode = mode, Configuration = source(new())};
        configuration.ServiceCollection.AddSingleton(iprojection, tprojection);
        configuration.ServiceCollection.AddSingleton(typeof(IProjection), tprojection);
        configuration.Projections.Add(tprojection);
        return configuration;
    }
}
