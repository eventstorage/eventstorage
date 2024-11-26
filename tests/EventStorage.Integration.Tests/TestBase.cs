using EventStorage.Configurations;
using EventStorage.Infrastructure;
using EventStorage.Unit.Tests.AggregateRoot;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Integration.Tests;

public class TestBase<T> where T : OrderAggregate
{
    protected IServiceProvider Container(EventStore store) => Configuration<T>.Container(store);
    protected IEventStorage<T> EventStorage(EventStore source) =>
        Container(source).GetRequiredService<IEventStorage<T>>();
}