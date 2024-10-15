using EventStorage.Configurations;
using EventStorage.Unit.Tests.AggregateRoot;
using FluentAssertions;

namespace EventStorage.Integration.Tests;

public class EventStorageTests : TestBase<OrderAggregate>
{
    [Theory]
    [InlineData(EventStore.SqlServer)]
    [InlineData(EventStore.AzureSql)]
    [InlineData(EventStore.PostgresSql)]
    public async Task WhenCreateOrRestore_ShouldCreateAndRestoreAggregate(EventStore source)
    {
        // Given
        var service = EventStorage(source);
        await service.InitSource();

        // When
        var aggregate = await service.CreateOrRestore();

        // Then
        Assert.NotNull(aggregate);
        aggregate.SourceId.Should().BeGreaterThan(0);
    }
    [Theory]
    [InlineData(EventStore.AzureSql)]
    [InlineData(EventStore.SqlServer)]
    [InlineData(EventStore.PostgresSql)]
    public async Task GivenPlacedOrder_WhenCommitting_ShouldCommitAggregate(EventStore source)
    {
        // Given
        var service = EventStorage(source);
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
    [InlineData(EventStore.AzureSql)]
    [InlineData(EventStore.SqlServer)]
    [InlineData(EventStore.PostgresSql)]
    public async Task GivenExistingSource_ShouldRestoreAggregate(EventStore source)
    {
        // Given
        var service = EventStorage(source);
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
    [InlineData(EventStore.AzureSql)]
    [InlineData(EventStore.SqlServer)]
    [InlineData(EventStore.PostgresSql)]
    public async Task GivenExistingSource_WhenConfirming_ShouldAppendEvent(EventStore source)
    {
        // Given
        var service = EventStorage(source);
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
    [InlineData(EventStore.AzureSql)]
    [InlineData(EventStore.SqlServer)]
    [InlineData(EventStore.PostgresSql)]
    public async Task GivenSource_ConfirmingTwice_ShouldAvoidAppendingEvent(EventStore source)
    {
        // Given
        var service = EventStorage(source);
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