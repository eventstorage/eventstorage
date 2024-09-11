# asynchandler

### A lightweight event sourcing framework designed for your event-store of choice.

[![Github follow](https://img.shields.io/badge/follow-asynchandler-bf9136?logo=github)](https://github.com/asynchandler)
[![Github follow](https://img.shields.io/badge/follow-eventsourcer-bf9136?logo=github)](https://github.com/eventsourcer)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![Nuget Package](https://badgen.net/nuget/v/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Nuget](https://badgen.net/nuget/dt/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Github follow](https://img.shields.io/badge/give_us_a-⭐-yellow?logo=github)](https://github.com/asynchandler/AsyncHandler.EventSourcing)
[![build Status](https://dev.azure.com/asynchandler/AsyncHandler.EventSourcing/_apis/build/status%2Fasynchandler.AsyncHandler.EventSourcing?branchName=main&label=azure%20pipes)](https://dev.azure.com/asynchandler/AsyncHandler.EventSourcing/_build/latest?definitionId=11&branchName=main)

<div align="left">
    <img src="assets/ah_radius.PNG" width="80" height="80" style="float:left;" alt="asynchandler">
</div>

### Overview

asynchandler is a high-performance event sourcing framework built for .Net that allows selecting event storage of choice. Combining consistency with schema flexibility, asynchandler aims to simplify event sourcing for everyone. Currently supports Azure Sql database and Sql Server, with Postgres and others in the upcoming releases.

### Environment setup

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

### Getting started

[![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
#### Install the package

###### Simply install `AsyncHandler.EventSourcing` package.

    dotnet add package AsyncHandler.EventSourcing

#### Configure your event source

###### Use `AddAsyncHandler` service collection extension.

```csharp
builder.Services.AddAsyncHandler(asynchandler =>
{
    asynchandler.AddEventSourcing(source =>
    {
        source.SelectEventSource(EventSources.AzureSql, connectionString);
    });
});
```

Select your event source of choice from `SelectEventSource`, currently AzureSql and SqlServer are supported.

Make sure you have defined your connection string from appsettings.

#### Define your aggregate
###### Add your aggregate with AggregateRoot

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

Please notice, these are early moments of asynchandler and the framework doesn't yet offer full event sourcing functionality.

### Give us a ⭐
If you are an event sourcer and love OSS, give [asynchandler](https://github.com/asynchandler/AsyncHandler.EventSourcing) a star. :purple_heart:

### License

This project is licensed under the terms of the [MIT](https://github.com/asynchandler/AsyncHandler.EventSourcing/blob/main/LICENSE) license.
