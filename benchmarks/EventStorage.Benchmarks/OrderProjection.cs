using EventStorage.Benchmarks.Events;
using EventStorage.Events;
using EventStorage.Projections;
using Redis.OM.Modeling;

namespace EventStorage.Benchmarks;
public record Order(string SourceId, OrderStatus Status, long Version);

public record OrderDetail(string SourceId, OrderStatus Status, long Version);

[Document(StorageType = StorageType.Json, Prefixes = ["OrderDocument"])]
public class OrderDocument
{
    [RedisIdField][Indexed]
    public string? SourceId { get; set; } = string.Empty;
    // [Searchable]
    public OrderStatus Status { get; set; }
    public long Version { get; set; }
}
public class OrderProjection : Projection<Order>
{
    public static Order Project(OrderPlaced orderPlaced) => 
        new(orderPlaced.SourceId?.ToString()?? "", OrderStatus.Placed, orderPlaced.Version);
    public static Order Project(OrderConfirmed orderConfirmed, Order order) =>
        order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}

public class OrderDetailProjection : Projection<OrderDetail>
{
    public static OrderDetail Project(OrderPlaced orderPlaced) =>
        new(orderPlaced.SourceId?.ToString()?? "", OrderStatus.Placed, orderPlaced.Version);
    public static OrderDetail Project(OrderConfirmed orderConfirmed, OrderDetail order) =>
        order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}

public class OrderDocumentProjection : Projection<OrderDocument>
{
    public static OrderDocument Project(OrderPlaced orderPlaced) => new()
    {
        SourceId = orderPlaced.SourceId?.ToString(),
        Version = orderPlaced.Version,
        Status = OrderStatus.Placed
    };
    public static OrderDocument Project(OrderConfirmed orderConfirmed, OrderDocument orderDocument)
    {
        orderDocument.Status = OrderStatus.Confirmed;
        orderDocument.Version = orderConfirmed.Version;
        return orderDocument;
    }
}