using EventStorage.Configurations;
using EventStorage.Repositories;
using EventStorage.Repositories.PostgreSql;
using EventStorage.Repositories.SqlServer;
using EventStorage.Unit.Tests.AggregateRoot;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Integration.Tests;

public class TestBase<T> where T : OrderAggregate
{
    private static readonly IServiceProvider _container = Configuration<T>.Container;
    public static IEventStorage<T> EventStorage(EventStore source) =>
        _container.GetRequiredKeyedService<IEventStorage<T>>(source);
    public static ISqlServerClient<T> AzureSqlClient =>
        _container.GetRequiredKeyedService<IRepository<T>>(EventStore.AzureSql).SqlServerClient;
    public static ISqlServerClient<T> SqlServerClient =>
        _container.GetRequiredKeyedService<IRepository<T>>(EventStore.SqlServer).SqlServerClient;
    public static IPostgreSqlClient<T> PostgreSqlClient =>
        _container.GetRequiredKeyedService<IRepository<T>>(EventStore.PostgresSql).PostgreSqlClient;
}