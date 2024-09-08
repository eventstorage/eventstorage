using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories.AzureSql;
using AsyncHandler.EventSourcing.Tests.Unit;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public class AzureSqlClientTests : TestBase
{
    [Fact]
    public async Task TestMsSqlClient()
    {
        // giveb
        var source = EventSources.SqlServer;
        var sp = BuildContainer(source);
        var conn = BuildConfiguration(source);
        var client = new AzureSqlClient<OrderAggregate>(conn, sp, source);
        await client.Init();

        //when
        var aggregate = await client.CreateOrRestore();

        // then
        Assert.NotNull(aggregate);
    }
    [Fact]
    public async Task TestAzureSqlClient()
    {
        // giveb
        var source = EventSources.AzureSql;
        var sp = BuildContainer(source);
        var conn = BuildConfiguration(source);
        var client = new AzureSqlClient<OrderAggregate>(conn, sp, source);
        await client.Init();

        //when
        var aggregate = await client.CreateOrRestore();

        // then
        Assert.NotNull(aggregate);
    }
}
