using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Benchmarks.Projections;

public class OrderDetailProjection : Projection<OrderDetail>
{
    public static OrderDetail Project(OrderPlaced orderPlaced) => 
        new(orderPlaced.SourceId?.ToString()?? "", OrderStatus.Placed, orderPlaced.Version);
    // public static OrderDetail Project(OrderDetail order, OrderConfirmed orderConfirmed) =>
        // order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}