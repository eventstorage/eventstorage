using AsyncHandler.EventSourcing.Events;
using FluentAssertions;

namespace AsyncHandler.EventSourcing.Tests.Unit;

public class AggregareRootTests
{
    [Fact]
    public async Task GivenPlaceOrder_ShouldPlaceOrder()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.PlaceOrder();

        // then
        aggregate.PendingEvents.Any().Should().BeTrue();
        aggregate.Version.Should().Be(1);
        aggregate.OrderStatus.Should().Be(OrderStatus.Placed);
        Func<SourceEvent,bool> predicate = (e) => typeof(OrderPlaced).IsAssignableFrom(e.GetType());
        aggregate.PendingEvents.Any(predicate).Should().Be(true);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task GivenPlaceOrder_ShouldApplyOrderPlaced()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.PlaceOrder();

        // then
        aggregate.OrderStatus.Should().Be(OrderStatus.Placed);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task WhenAppendingEvent_ShouldBumpVersion()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.PlaceOrder();

        // then
        aggregate.Version.Should().Be(1);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task GivenConfirmOrder_ShouldApplyOrderConfirmed()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.ConfirmOrder();

        // then
        aggregate.OrderStatus.Should().Be(OrderStatus.Confirmed);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task GivenDuplicatePlaceOrder_ShouldAppendOnlyOneEvent()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.PlaceOrder();
        aggregate.PlaceOrder();

        // then
        aggregate.PendingEvents.Count().Should().Be(1);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task WhenCommittingAggregate_ShouldCommitEvents()
    {
        // given
        var aggregate = new OrderAggregate(1);
        aggregate.PlaceOrder();
        aggregate.ConfirmOrder();

        // when
        aggregate.CommitPendingEvents();

        // then
        aggregate.PendingEvents.Count().Should().Be(0);
        aggregate.EventStream.Count().Should().Be(2);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task WhenRaisingMultipleEvents_ShouldAppendAll()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.AppendMultiple();

        // then
        aggregate.PendingEvents.Count().Should().BeGreaterThan(1);
        await Task.CompletedTask;
    }
    [Fact]
    public async Task WhenAppendingEvents_ShouldSetCausationId()
    {
        // given
        var aggregate = new OrderAggregate(1);

        // when
        aggregate.PlaceOrder();
        aggregate.ConfirmOrder();

        // then
        aggregate.PendingEvents.First().Id.Should().Be(aggregate.PendingEvents.Last().CausationId);
        await Task.CompletedTask;
    }
}