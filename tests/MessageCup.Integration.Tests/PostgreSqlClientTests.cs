using AsyncHandler.EventSourcing.Tests.Unit;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public class PostgreSqlClientTests : TestBase<OrderAggregate>
{
    [Fact]
    public async Task WhilePostgreSqlSource_ShouldInitAndCreateEventStream()
    {
        // giveb
        var client = PostgreSqlClient;
        await client.Init();

        //when
        var aggregate = await client.CreateOrRestore();

        // then
        Assert.NotNull(aggregate);
    }
    [Fact]
    public async Task GivenAggregate_ShouldCommitEventSource()
    {
        // giveb
        var client = PostgreSqlClient;
        await client.Init();
        var aggregate = await client.CreateOrRestore();

        //when
        await client.Commit(aggregate);

        // then
        Assert.Empty(aggregate.PendingEvents);
    }
}
