# eventstorage

### A lightweight event sourcing framework for .NET with event storage of choice.

[![Github follow](https://img.shields.io/badge/follow-eventstorage-bf9136?logo=github)](https://github.com/eventstorage)
[![Nuget Package](https://badgen.net/nuget/v/eventstorage)](https://www.nuget.org/packages/eventstorage)
[![Nuget](https://badgen.net/nuget/dt/eventstorage)](https://www.nuget.org/packages/eventstorage)
[![Github follow](https://img.shields.io/badge/give_us_a-⭐-yellow?logo=github)](https://github.com/eventstorage/eventstorage)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![build Status](https://dev.azure.com/eventstorage/eventstorage/_apis/build/status%2Feventstorage?branchName=main&label=azure%20pipes)](https://dev.azure.com/eventstorage/eventstorage/_build/latest?definitionId=1&branchName=main)

<div align="left">
    <img src=".assets/es.png" width="80" height="80" style="float:left;" alt="eventstorage">
</div>

## Overview

eventstorage is a high-performance event sourced infrastructure out of the box, powered by latest
innovative C# that allows selecting event storage of choice with `Azure Sql`, `Postgres` and `Sql Server`,
and offers multiple projection modes with support to `Redis` for high-performance asynchronous projections.

#### Key benefits of eventstorage
* Runs plain sql resulting in high-performance storage infrastructure 
* Flexible schema gains with denormalization and throwing away ORM
* Identifies aggregates with `GUID` and `long` and allows switching back and forth
* Lightning-fast storage operations by selecting `long` aggregate identifier
* Allows event storage selection with `Azure Sql`, `Postgre Sql` and `Sql Server`
* Offers transient/runtime, ACID-compliant and async projections modes
* Allows projection source as either selected storage or high-performance `Redis`
* Lightweight asynchronous projection engine that polls for async projections
* Restores projections at startup ensuring consistency without blocking business
* Designed to survive failures and prevents possible inconsistencies

## Environment setup

[![My Skills](https://skillicons.dev/icons?i=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

eventstorage runs on .Net 8 and requires the SDK installed:

    https://dotnet.microsoft.com/en-us/download/dotnet/8.0

[![My Skills](https://skillicons.dev/icons?i=docker)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

Use docker to run mssql or postgres databases, execute `docker-compose` or `docker run`:
```sh
docker compose --project-name eventstorage up -d
```
`Postgres`
```sh
docker run --name some-postgres -p 5432:5432 -e POSTGRES_PASSWORD=postgres -d postgres
```
`Sql Server`
```sh
docker run --name some-mssql -p 1433:1433 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=sysadmin@1234" -d mcr.microsoft.com/mssql/server:2019-latest
```
Optionally run `Redis` for high-performance projections:
```sh
docker run -d --name redis-stack -p 6379:6379 -p 8001:8001 redis/redis-stack:latest
```

## Getting started

[![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
#### Install the package

###### Simply install `EventStorage` package.
```sh
dotnet add package EventStorage --prerelease
```
#### Configure event storage

###### Use `AddEventStorage` service collection extension.

```csharp
builder.Services.AddEventStorage(storage =>
{
    storage.Schema = "es";
    storage.AddEventSource(source =>
    {
        source.Select(EventStore.PostgresSql, connectionString);
    });
});
```
Use configuration options to select your event storage of choice and make sure connection string is defined.

#### Create aggregate root
###### Add your aggregate with `EventSource<TId>`

```csharp
public class OrderBooking : EventSource<long> // or Guid
{
    public OrderStatus OrderStatus { get; private set; }
    public void Apply(OrderPlaced e)
    {
        OrderStatus = OrderStatus.Placed;
    }
    public void PlaceOrder(PlaceOrder command)
    {
        if(OrderStatus == OrderStatus.Placed)
            return;
        RaiseEvent(new OrderPlaced(command));
    }
}

public enum OrderStatus
{
    Draft = 0,
    Placed = 1,
    Confirmed = 2,
}
public record PlaceOrder(string ProductName, int Quantity, string UserId);
public record OrderPlaced(PlaceOrder Command) : SourcedEvent;
```
`EventSource<TId>` allows selecting `long` or `Guid` to identify event streams.
While selecting `long` offers lightnening-fast queries and is human readable, we can always switch back and forth between the two!

#### Use `IEventStorage<T>` service

```csharp
app.MapPost("api/placeorder", 
async(IEventStorage<OrderBooking> eventStorage, PlaceOrder command) =>
{
    var aggregate = await eventStorage.CreateOrRestore();
    aggregate.PlaceOrder(command);

    await eventStorage.Commit(aggregate);
    return Results.Ok(aggregate.SourceId);
});
```

Similar to placing order, add two more methods to the aggregate to confirm an order, `ConfirmOrder` command and `Apply(OrderConfirmed)` method:

```csharp
app.MapPost("api/confirmorder/{orderId}", 
async(IEventStorage<OrderBooking> eventStorage, string orderId, ConfirmOrder command) =>
{
    var aggregate = await eventStorage.CreateOrRestore(orderId);
    aggregate.ConfirmOrder(command);

    await eventStorage.Commit(aggregate);
});
```

#### Configure projections
Transient (runtime), consistent and async projection modes are supported.

```csharp
eventstorage.AddEventSource(source =>
{
    source.Select(EventStore.SqlServer, connectionString)
    .Project<OrderProjection>(ProjectionMode.Transient)
    .Project<OrderDetailProjection>(ProjectionMode.Consistent)
    .Project<OrderInfoProjection>(ProjectionMode.Async)
    .Project<OrderDocumentProjection>(ProjectionMode.Async, src => src.Redis("redis://localhost"));
});
```
When projection mode set to async, optional projection source can be selected and is by default set to selected event store.

##### Sample projection
```csharp
public record Order(string SourceId, OrderStatus Status, long Version);

public class OrderProjection : Projection<Order>
{
    public static Order Project(OrderPlaced orderPlaced) => 
        new(orderPlaced.SourceId?.ToString()?? "", OrderStatus.Placed, orderPlaced.Version);
    public static Order Project(Order order, OrderConfirmed orderConfirmed) =>
        order with { Status = OrderStatus.Confirmed, Version = orderConfirmed.Version };
}
```
##### Sample Redis projection
```csharp
[Document(StorageType = StorageType.Json, Prefixes = ["OrderDocument"])]
public class OrderDocument
{
    [RedisIdField][Indexed]
    public string? SourceId { get; set; } = string.Empty;
    // [Searchable]
    public OrderStatus Status { get; set; }
    public long Version { get; set; }
}

public class OrderDocumentProjection : Projection<OrderDocument>
{
    public static OrderDocument Project(OrderPlaced orderPlaced) => new()
    {
        SourceId = orderPlaced.SourceId?.ToString(),
        Version = orderPlaced.Version,
        Status = OrderStatus.Placed
    };
    public static OrderDocument Project(OrderDocument orderDocument, OrderConfirmed orderConfirmed)
    {
        orderDocument.Status = OrderStatus.Confirmed;
        orderDocument.Version = orderConfirmed.Version;
        return orderDocument;
    }
}
```

#### Define endpoints to project
```csharp
// endpoint for transient OrderProjection
app.MapGet("api/order/{orderId}",
async(IEventStorage<OrderBooking> eventStorage, string orderId) =>
{
    var order = await eventStorage.Project<Order>(orderId);
    return Results.Ok(order);
});
// endpoint for async OrderDocumentProjection on Redis
app.MapGet("api/orderDocument/{orderId}",
    async(IEventStorage<OrderBooking> eventStorage, string orderId) =>
    await eventStorage.Project<OrderDocument>(orderId)
);
```

## Documentation
For detailed walkthrough and guides, visit our beautiful [documentation](https://eventstorage.github.io) site.


## Give us a ⭐
If you are an event sourcer and love OSS, give [eventstorage](https://github.com/eventstorage/eventstorage) a star. :purple_heart:

### License

This project is licensed under the terms of [MIT](https://github.com/eventstorage/eventstorage/blob/main/LICENSE) license.
