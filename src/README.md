# asynchandler

### A lightweight event sourcing framework for .Net with event-store of choice.

[![Github follow](https://img.shields.io/badge/follow-asynchandler-bf9136?logo=github)](https://github.com/asynchandler)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![Nuget Package](https://badgen.net/nuget/v/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Nuget](https://badgen.net/nuget/dt/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Github follow](https://img.shields.io/badge/give_us_a-⭐-yellow?logo=github)](https://github.com/asynchandler/AsyncHandler.EventSourcing)
[![build Status](https://dev.azure.com/asynchandler/AsyncHandler.EventSourcing/_apis/build/status%2Fasynchandler.AsyncHandler.EventSourcing?branchName=main&label=azure%20pipes)](https://dev.azure.com/asynchandler/AsyncHandler.EventSourcing/_build/latest?definitionId=11&branchName=main)


<!-- <div align="left">
    <img src="../../assets/ah_radius.png" width="80" height="80" style="float:left;" alt="asynchandler">
</div> -->

### Overview

asynchandler is a high-performance event sourcing framework built for .Net that allows selecting event storage of choice. Combining consistency with schema flexibility, asynchandler aims to make event sourcing simplified for everyone. Currently supports Azure Sql, Postgres and Sql Server.

### Prerequisites

<!-- [![My Skills](https://skillicons.dev/icons?i=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->

asynchandler runs on the stable release of .Net 8 and requires the SDK installed.

    https://dotnet.microsoft.com/en-us/download/dotnet/8.0

<!-- [![My Skills](https://skillicons.dev/icons?i=docker)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->

Use docker to run sqlserver or postgres databases, execute `docker-compose`.

    docker compose --project-name asynchandler up -d

### Usage

<!-- [![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->


Simply install `AsyncHandler.EventSourcing` package.

    dotnet add package AsyncHandler.EventSourcing

Use `AddAsyncHandler` service collection extension.

```csharp
var connectionString = builder.Configuration["postgresqlsecret"]??
    throw new Exception("No connection defined");

builder.Services.AddAsyncHandler(asynchandler =>
{
    asynchandler.Schema = "ah";
    asynchandler.AddEventSourcing(source =>
    {
        source.SelectEventSource(EventSources.PostgresSql, connectionString);
    });
});
```

Select your event source of choice from `SelectEventSource`.
Make sure you have defined your connection string.

#### Define your aggregate
Add your aggregate with AggregateRoot

```csharp
public class OrderBookingAggregate : AggregateRoot<long> // or Guid
{
    public OrderStatus OrderStatus { get; private set; }
    protected override void Apply(SourcedEvent e)
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
AggregateRoot allows selecting `long` or `Guid` for sourceId, selecting `long` offers lightnening-fast queries. 

To continue please visit our [GitHub](https://github.com/asynchandler/AsyncHandler.EventSourcing) handle.

### Give us a star ⭐
If you are an event sourcer and love OSS, give [asynchandler](https://github.com/asynchandler/AsyncHandler.EventSourcing) a star. :purple_heart:

### License

This project is licensed under the terms of the [MIT](https://github.com/asynchandler/AsyncHandler.EventSourcing/blob/main/LICENSE) license.
