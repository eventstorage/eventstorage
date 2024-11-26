using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Repositories.PostgreSql;
using EventStorage.Repositories.SqlServer;
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
        ArgumentNullException.ThrowIfNull(aggregateType);
        
        EventStorageSchema clientSchema = source switch
        {
            EventStore.PostgresSql => new PostgreSqlSchema(configuration.Schema),
            EventStore.AzureSql => new AzureSqlSchema(configuration.Schema),
            _ => new SqlServerSchema(configuration.Schema)
        };
        configuration.ServiceCollection.AddSingleton(typeof(IEventStorageSchema), clientSchema);

        var eventStorageType = typeof(IEventStorage<>).MakeGenericType(aggregateType);
        var postgresClientType = typeof(PostgreSqlClient<>).MakeGenericType(aggregateType);
        var mssqlClientType = typeof(SqlServerClient<>).MakeGenericType(aggregateType);
        var client = source switch
        {
            EventStore.PostgresSql => postgresClientType,
            _ => mssqlClientType
        };
        var storage = Activator.CreateInstance(client, configuration.Sp, connectionString)?? default!;
        configuration.ServiceCollection.AddScoped(eventStorageType, sp => storage );
        configuration.ConnectionString = connectionString;
        configuration.Source = source;
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
        var iprojection = typeof(TProjection).GetInterfaces().Last();
        source ??= (config) => new();
        var tprojection = new TProjection { Mode = mode, Configuration = source(new())};
        configuration.ServiceCollection.AddSingleton(iprojection, tprojection);
        configuration.ServiceCollection.AddSingleton(typeof(IProjection), tprojection);
        configuration.Projections.Add(tprojection);
        return configuration;
    }
}
