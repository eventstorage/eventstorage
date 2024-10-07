using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Tests.Unit;
using FluentAssertions;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public class EventSourceTests : TestBase<OrderAggregate>
{
    [Theory]
    [InlineData(EventSources.SqlServer)]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.PostgresSql)]
    public async Task WhenCreateOrRestore_ShouldCreateAndRestoreAggregate(EventSources source)
    {
        // Given
        var service = EventSource(source);
        await service.InitSource();
    
        // When
        var aggregate = await service.CreateOrRestore();
    
        // Then
        Assert.NotNull(aggregate);
        aggregate.SourceId.Should().BeGreaterThan(0);
    }
    [Theory]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.SqlServer)]
    [InlineData(EventSources.PostgresSql)]
    public async Task GivenPlacedOrder_WhenCommitting_ShouldCommitAggregate(EventSources source)
    {
        // Given
        var service = EventSource(source);
        await service.InitSource();
        var aggregate = await service.CreateOrRestore();
    
        // When
        aggregate.PlaceOrder();
        await service.Commit(aggregate);
    
        // Then
        aggregate.PendingEvents.Count().Should().Be(0);
        aggregate.EventStream.Count().Should().BeGreaterThan(0);
    }
    [Theory]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.SqlServer)]
    [InlineData(EventSources.PostgresSql)]
    public async Task GivenExistingSource_ShouldRestoreAggregate(EventSources source)
    {
        // Given
        var service = EventSource(source);
        await service.InitSource();
        var expectedAggregate = await service.CreateOrRestore();
        expectedAggregate.PlaceOrder();
        await service.Commit(expectedAggregate);
    
        // When
        var aggregate = await service.CreateOrRestore(expectedAggregate.SourceId.ToString());
    
        // Then
        Assert.Equal(expectedAggregate.SourceId, aggregate.SourceId);
        aggregate.EventStream.Count().Should().BeGreaterThan(0);
    }
    [Theory]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.SqlServer)]
    [InlineData(EventSources.PostgresSql)]
    public async Task GivenExistingSource_WhenConfirming_ShouldAppendEvent(EventSources source)
    {
        // Given
        var service = EventSource(source);
        await service.InitSource();
        var aggregate = await service.CreateOrRestore();
        aggregate.PlaceOrder();
        await service.Commit(aggregate);
    
        // When
        var result = await service.CreateOrRestore(aggregate.SourceId.ToString());
        result.ConfirmOrder();
        await service.Commit(result);
    
        // Then
        result.EventStream.Count().Should().Be(2);
        result.Version.Should().Be(2);
    }
    [Theory]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.SqlServer)]
    [InlineData(EventSources.PostgresSql)]
    public async Task GivenSource_ConfirmingTwice_ShouldAvoidAppendingEvent(EventSources source)
    {
        // Given
        var service = EventSource(source);
        await service.InitSource();
        var aggregate = await service.CreateOrRestore();

        // When
        aggregate.ConfirmOrder();
        aggregate.ConfirmOrder();
        await service.Commit(aggregate);
    
        // Then
        aggregate.EventStream.Count().Should().Be(1);
        aggregate.Version.Should().Be(1);
    }
}