using EventStorage.Unit.Tests.AggregateRoot;

namespace EventStorage.Integration.Tests;

public class SqlServerClientTests : TestBase<OrderAggregate>
{
    [Fact]
    public async Task WhileSqlServerSource_ShouldInitAndCreateEventStream()
    {
        // giveb
        var client = SqlServerClient;
        await client.Init();

        //when
        var aggregate = await client.CreateOrRestore();

        // then
        Assert.NotNull(aggregate);
    }
    [Fact]
    public async Task GivenSqlServerAggregateSource_ShouldCommitEventSource()
    {
        // giveb
        var client = SqlServerClient;
        await client.Init();
        var aggregate = await client.CreateOrRestore();

        //when
        await client.Commit(aggregate);

        // then
        Assert.Empty(aggregate.PendingEvents);
    }
    [Fact]
    public async Task WhileAzureSqlSource_ShouldInitAndCreateEventStream()
    {
        // giveb
        var client = AzureSqlClient;
        await client.Init();

        //when
        var aggregate = await client.CreateOrRestore();

        // then
        Assert.NotNull(aggregate);
    }
    [Fact]
    public async Task GivenAzureSqlAggregateSource_ShouldCommitEventSource()
    {
        // giveb
        var client = AzureSqlClient;
        await client.Init();
        var aggregate = await client.CreateOrRestore();

        //when
        await client.Commit(aggregate);

        // then
        Assert.Empty(aggregate.PendingEvents);
    }
}
