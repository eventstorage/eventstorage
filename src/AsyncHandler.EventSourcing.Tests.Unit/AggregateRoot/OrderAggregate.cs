using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Extensions;

namespace AsyncHandler.EventSourcing.Tests.Unit;

public class OrderAggregate(long sourceId) : AggregateRoot(sourceId)
{
    public OrderStatus OrderStatus { get; set; }
    protected override void Apply(SourceEvent e)
    {
        this.InvokeApply(e);
    }
    public void Apply(OrderPlaced e)
    {
        OrderStatus = OrderStatus.Placed;
    }
    public void Apply(OrderConfirmed e)
    {
        OrderStatus = OrderStatus.Confirmed;
    }
    public void PlaceOrder()
    {
        if(OrderStatus == OrderStatus.Placed)
            return;
        RaiseEvent(new OrderPlaced());
    }
    public void ConfirmOrder()
    {
        if(OrderStatus == OrderStatus.Confirmed)
            return;
        RaiseEvent(new OrderConfirmed());
    }
    public void AppendMultiple()
    {
        RaiseEvent(new OrderConfirmed());
        RaiseEvent(new OrderPlaced());
    }
}

public record OrderPlaced : SourceEvent;
public record OrderConfirmed : SourceEvent;
public enum OrderStatus
{
    Draft = 0,
    Placed = 1,
    Confirmed = 2,
}