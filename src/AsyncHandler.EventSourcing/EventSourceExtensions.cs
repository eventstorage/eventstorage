using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Projections;
using AsyncHandler.EventSourcing.Repositories;
using AsyncHandler.EventSourcing.Repositories.AzureSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing;

public static class EventSourceExtensions
{
    public static EventSourceConfiguration SelectEventSource(
        this EventSourceConfiguration configuration,
        EventSources source,
        string connectionString)
    {
        configuration.ServiceCollection.AddTransient<IRepository<AggregateRoot>>(sp =>
        {
            var repository = new Repository<AggregateRoot>(connectionString, sp);
            Task.Run(() => source switch
            {
                EventSources.AzureSql => repository.AzureSqlClient.InitSource(),
                EventSources.PostgresSql => repository.PostgreSqlClient.InitSource(),
                EventSources.SQLServer => repository.SqlServerClient.InitSource(),
                _ => Task.CompletedTask,
            });
            return repository;
        });
        configuration.ServiceCollection.AddTransient<IEventSource<AggregateRoot>>(sp =>
        {
            return new EventSource<AggregateRoot>(sp.GetRequiredService<IRepository<AggregateRoot>>(), source);
        });
        return configuration;
    }
    public static void AddProjection<T>(
        this EventSourceConfiguration configuration,
        ProjectionMode projectionMode)
    {
        
    }
}
