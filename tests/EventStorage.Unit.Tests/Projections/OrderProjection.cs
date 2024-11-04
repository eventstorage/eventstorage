using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Unit.Tests.AggregateRoot;
using Redis.OM.Modeling;

namespace EventStorage.Unit.Tests.Projections;
public record Order(string SourceId, OrderStatus Status, long Version);

public record OrderDetail(string SourceId, OrderStatus Status, long Version);

[Document(StorageType = StorageType.Json)]
public class OrderDocument
{
    [RedisIdField]
    [Indexed]
    public string SourceId { get; set; } = string.Empty;
    [Searchable]
    public OrderStatus Status { get; set; }
    public long Version { get; set; }
}
public class OrderProjection : Projection<Order>
{
    public static Order Project(OrderPlaced orderPlaced) =>
        new(orderPlaced.SourceId?.ToString() ?? "", OrderStatus.Placed, orderPlaced.Version);
    public static Order Project(Order order, OrderConfirmed orderConfirmed) =>
        order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}