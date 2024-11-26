using EventStorage.Configurations;
using EventStorage.Repositories;
using EventStorage.Repositories.PostgreSql;
using EventStorage.Repositories.SqlServer;
using EventStorage.Unit.Tests.AggregateRoot;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Integration.Tests;

public class TestBase<T> where T : OrderAggregate
{
    private readonly IServiceProvider _container = new Configuration<T>().Container;
    public IEventStorage<T> EventStorage(EventStore source) => source switch
    {
        EventStore.AzureSql => _container.GetServices<IEventStorage<T>>().First(),
        EventStore.PostgresSql => _container.GetServices<IEventStorage<T>>().Skip(1).First(),
        _ => _container.GetServices<IEventStorage<T>>().Last()
    };
}