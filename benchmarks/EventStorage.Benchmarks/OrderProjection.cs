using EventStorage.Benchmarks.Events;
using EventStorage.Events;
using EventStorage.Projections;
using Redis.OM.Modeling;

namespace EventStorage.Benchmarks;
public record Order(string SourceId, OrderStatus Status, long Version);

public record OrderDetail(string SourceId, OrderStatus Status, long Version);

[Document(StorageType = StorageType.Json)]
public class OrderDocument
{
    [RedisIdField][Indexed]
    public string SourceId { get; set; } = string.Empty;
    [Searchable]
    public OrderStatus Status { get; set; }
    public long Version { get; set; }
}
public class OrderProjection : Projection<Order,OrderBookingLong>
{
    public static Order Project(OrderPlaced orderPlaced) => 
        new(orderPlaced.SourceId?.ToString()?? "", OrderStatus.Placed, orderPlaced.Version);
    public static Order Project(Order order, OrderConfirmed orderConfirmed) =>
        order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}

public class OrderDetailProjection : Projection<OrderDetail,OrderBookingLong>
{
    public static OrderDetail Project(OrderPlaced orderPlaced) =>
        new(orderPlaced.SourceId?.ToString()?? "", OrderStatus.Placed, orderPlaced.Version);
    public static OrderDetail Project(OrderDetail order, OrderConfirmed orderConfirmed) =>
        order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}