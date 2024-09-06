# asynchandler

### A lightweight event sourcing framework designed for your event-store of choice.

[![Github follow](https://img.shields.io/badge/follow-asynchandler-red?logo=github)](https://github.com/asynchandler)
[![Github follow](https://img.shields.io/badge/follow-eventsourcer-red?logo=github)](https://github.com/eventsourcer)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![Nuget Package](https://badgen.net/nuget/v/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Nuget](https://badgen.net/nuget/dt/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Github follow](https://img.shields.io/badge/give_us_a-*-red?logo=github)](https://github.com/asynchandler/AsyncHandler.EventSourcing)

<div align="left">
    <img src="assets/github.png" alt="marten logo" width="80" height="80" style="float:left;">
</div>

### Overview

asynchandler is a high-performance event sourcing framework built for .Net, aiming to simplify event sourcing for everyone. asynchandler combines consistency with schema flexibility, and integrates easily with event storage vendors. Currently supports Azure Sql database and Sql Server, with Postgres in the upcoming releases.

### Setup environment

[![My Skills](https://skillicons.dev/icons?i=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

asynchandler runs on the stable release of .Net 8 and requires the SDK installed:

    https://dotnet.microsoft.com/en-us/download/dotnet/8.0

[![My Skills](https://skillicons.dev/icons?i=docker)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

Use docker to run sqlserver or postgres databases, execute `docker-compose` or `docker run`:

    docker compose --project-name some-name up -d

`Postgres`

    docker run --name some-postgres -p 5432:5432 -e POSTGRES_PASSWORD=mysecretpassword -d postgres

`SqlServer`

    docker run --name some-mssql -p 1433:1433 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=password" -d mcr.microsoft.com/mssql/server:2019-latest

### Usage and code

[![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
##### Install the package

Simply install `AsyncHandler.EventSourcing` package:

    dotnet add package AsyncHandler.EventSourcing --prerelease

##### Configure your event source
asynchandler uses `AddAsyncHandler` service collection extension as the entry point to configure your event source.

```csharp
var connectionString = builder.Configuration["AzureSqlDatabase"] ?? 
    throw new Exception("No connection defined");

builder.Services.AddAsyncHandler(asynchandler =>
{
    asynchandler.AddEventSourcing(source =>
    {
        source.SelectEventSource(EventSources.AzureSql, connectionString)
        .AddProjection<Projection>(ProjectionMode.Async);
    })
    .EnableTransactionalOutbox(MessageBus.Kafka, "");
});
```

Select your event source of choice from `SelectEventSource`, currently AzureSql and SqlServer are supported.

Make sure you have defined your connection string from appsettings.

#### Define your aggregate

```csharp
public class OrderBookingAggregate(long orderId) : AggregateRoot(orderId)
{
    public OrderStatus OrderStatus { get; private set; }
    protected override void Apply(SourceEvent e)
    {
        this.InvokeApply(e);
    }
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
```

#### Use `IEventSource<T>` service

```csharp
app.MapPost("api/placeorder", 
async(IEventSource<OrderBookingAggregate> eventSource, PlaceOrder command) =>
{
    var aggregate = await eventSource.CreateOrRestore();
    aggregate.PlaceOrder(command);

    await eventSource.Commit(aggregate);
});
```

Add two more methods to your aggregate to confirm an order, `ConfirmOrder` and `Apply(OrderConfirmed)`:

```csharp
app.MapPost("api/confirmorder/{orderId}", 
async(IEventSource<OrderBookingAggregate> eventSource, long orderId, ConfirmOrder command) =>
{
    var aggregate = await eventSource.CreateOrRestore(orderId);
    aggregate.ConfirmOrder(command);

    await eventSource.Commit(aggregate);
});
```

Please notice, this is a prelease and doesn't yet offer full event sourcing functionality.

### Give us a star ⭐
If you are an event sourcer and love OSS, give [us](https://github.com/asynchandler/AsyncHandler.EventSourcing) a star. :purple_heart:

### License

This project is licensed under the terms of the [MIT](https://github.com/asynchandler/AsyncHandler.EventSourcing/blob/main/LICENSE) license.
