using AsyncHandler.EventSourcing.Tests.Unit;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public class AzureSqlClientTests : TestBase<OrderAggregate>
{
    [Fact]
    public async Task GivenSqlServerSource_ShouldCreateEventStream()
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
    public async Task GivenAzureSqlSource_ShouldCreateEventStream()
    {
        // giveb
        var client = AzureSqlClient;
        await client.Init();

        //when
        var aggregate = await client.CreateOrRestore();

        // then
        Assert.NotNull(aggregate);
    }
}
