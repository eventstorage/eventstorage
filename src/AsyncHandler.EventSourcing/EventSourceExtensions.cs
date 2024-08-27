using System.Reflection;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Extensions;
using AsyncHandler.EventSourcing.Projections;
using AsyncHandler.EventSourcing.Repositories;
using Microsoft.Extensions.DependencyInjection;

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
        
        Type repositoryInterfaceType = typeof(IRepository<>).MakeGenericType(aggregateType);
        Type repositoryType = typeof(Repository<>).MakeGenericType(aggregateType);
        #pragma warning disable CS8603
        configuration.ServiceCollection.AddTransient(repositoryInterfaceType, sp =>
        {
            var repository = Activator.CreateInstance(repositoryType, connectionString, sp);
            // configuration.ServiceCollection.AddSingleton<IHostedService>(sp => new InitSource(repository));
            // Task.Run(() => source switch
            // {
            //     EventSources.AzureSql => repository.AzureSqlClient.InitSource(),
            //     EventSources.PostgresSql => repository.PostgreSqlClient.InitSource(),
            //     EventSources.SQLServer => repository.SqlServerClient.InitSource(),
            //     _ => Task.CompletedTask,
            // });
            return repository;
        });
        Type eventSourceInterfaceType = typeof(IEventSource<>).MakeGenericType(aggregateType);
        Type eventSourceType = typeof(EventSource<>).MakeGenericType(aggregateType);
        configuration.ServiceCollection.AddTransient(eventSourceInterfaceType, sp =>
        {
            return Activator.CreateInstance(eventSourceType, sp.GetRequiredService<IRepository<AggregateRoot>>(), source);
        });
        return configuration;
    }
    public static void AddProjection<T>(
        this EventSourceConfiguration configuration,
        ProjectionMode projectionMode)
    {
        
    }
}
