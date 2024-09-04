using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Tests.Unit;
using FluentAssertions;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public class EventSourceTests : TestBase
{
    private static EventSources Source => EventSources.AzureSql;
    private readonly IEventSource<OrderAggregate> _eventSource = GetEventSource(Source);
    [Fact]
    public async Task WhenCreateOrRestore_ShouldCreateAndRestoreAggregate()
    {
        // Given
        await _eventSource.InitSource();
    
        // When
        var aggregate = await _eventSource.CreateOrRestore();
    
        // Then
        Assert.NotNull(aggregate);
        aggregate.SourceId.Should().BeGreaterThan(0);
    }
    [Fact]
    public async Task GivenPlacedOrder_WhenCommitting_ShouldCommitAggregate()
    {
        // Given
        await _eventSource.InitSource();
        var aggregate = await _eventSource.CreateOrRestore();
    
        // When
        aggregate.PlaceOrder();
        await _eventSource.Commit(aggregate);
    
        // Then
        aggregate.PendingEvents.Count().Should().Be(0);
        aggregate.EventStream.Count().Should().BeGreaterThan(0);
    }
    [Fact]
    public async Task GivenExistingSource_ShouldRestoreAggregate()
    {
        // Given
        await _eventSource.InitSource();
        var expectedAggregate = await _eventSource.CreateOrRestore();
        expectedAggregate.PlaceOrder();
        await _eventSource.Commit(expectedAggregate);
    
        // When
        var aggregate = await _eventSource.CreateOrRestore(expectedAggregate.SourceId);
    
        // Then
        Assert.Equal(expectedAggregate.SourceId, aggregate.SourceId);
        aggregate.EventStream.Count().Should().BeGreaterThan(0);
    }
    [Fact]
    public async Task GivenExistingSource_WhenConfirming_ShouldAppendEvent()
    {
        // Given
        await _eventSource.InitSource();
        var aggregate = await _eventSource.CreateOrRestore();
        aggregate.PlaceOrder();
        await _eventSource.Commit(aggregate);
    
        // When
        var source = await _eventSource.CreateOrRestore(aggregate.SourceId);
        source.ConfirmOrder();
        await _eventSource.Commit(source);
    
        // Then
        source.EventStream.Count().Should().Be(2);
        source.Version.Should().Be(2);
    }
    [Fact]
    public async Task GivenSource_ConfirmingTwice_ShouldAvoidAppendingEvent()
    {
        // Given
        await _eventSource.InitSource();
        var source = await _eventSource.CreateOrRestore();

        // When
        source.ConfirmOrder();
        source.ConfirmOrder();
        await _eventSource.Commit(source);
    
        // Then
        source.EventStream.Count().Should().Be(1);
        source.Version.Should().Be(1);
    }
}