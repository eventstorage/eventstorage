using EventStorage.AggregateRoot;
using EventStorage.Benchmarks.Commands;
using EventStorage.Benchmarks.Events;

namespace EventStorage.Benchmarks;
public class OrderBooking: EventSource<long>
{
    public OrderStatus OrderStatus { get; private set; }
    public void Apply(OrderPlaced e)
    {
        OrderStatus = OrderStatus.Placed;
    }
    public void Apply(OrderConfirmed e)
    {
        OrderStatus = OrderStatus.Confirmed;
    }
    public void PlaceOrder(PlaceOrder command)
    {
        if(OrderStatus == OrderStatus.Placed)
            return;
        RaiseEvent(new OrderPlaced());
    }
    public void ConfirmOrder(ConfirmOrder command)
    {
        if( OrderStatus == OrderStatus.Confirmed)
            return;
        RaiseEvent(new OrderConfirmed());
    }
}
