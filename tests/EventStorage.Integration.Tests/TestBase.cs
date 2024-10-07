using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories;
using AsyncHandler.EventSourcing.Repositories.PostgreSql;
using AsyncHandler.EventSourcing.Repositories.SqlServer;
using AsyncHandler.EventSourcing.Tests.Unit;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public class TestBase<T> where T : OrderAggregate
{
    private static readonly IServiceProvider _container = Configuration<T>.Container;
    public static IEventSource<T> EventSource(EventSources source) =>
        _container.GetRequiredKeyedService<IEventSource<T>>(source);
    public static ISqlServerClient<T> AzureSqlClient =>
        _container.GetRequiredKeyedService<IRepository<T>>(EventSources.AzureSql).SqlServerClient;
    public static ISqlServerClient<T> SqlServerClient =>
        _container.GetRequiredKeyedService<IRepository<T>>(EventSources.SqlServer).SqlServerClient;
    public static IPostgreSqlClient<T> PostgreSqlClient =>
        _container.GetRequiredKeyedService<IRepository<T>>(EventSources.PostgresSql).PostgreSqlClient;
}