// using EventStorage.Events;

// namespace EventStorage.Projections;

// public class TestProjection : IProjection<Order>
// {
//     public static Order Project(Order order, OrderPlaced e) =>
//         order with { Id = e.Id, OrderStatus = OrderStatus.Placed };
// }



// public record Order(string Id, OrderStatus OrderStatus);
// public record OrderPlaced(string Id);
// public enum OrderStatus
// {
//     Confirmed,
//     Placed
// }