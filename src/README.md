# eventstorage

### A lightweight event sourcing framework for .Net with event storage of choice.

[![Github follow](https://img.shields.io/badge/follow-eventstorage-bf9136?logo=github)](https://github.com/eventstorage)
[![Nuget Package](https://badgen.net/nuget/v/eventstorage)](https://www.nuget.org/packages/eventstorage)
[![Nuget](https://badgen.net/nuget/dt/eventstorage)](https://www.nuget.org/packages/eventstorage)
[![Github follow](https://img.shields.io/badge/give_us_a-⭐-yellow?logo=github)](https://github.com/eventstorage/eventstorage)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![build Status](https://dev.azure.com/eventstorage/eventstorage/_apis/build/status%2Feventstorage?branchName=main&label=azure%20pipes)](https://dev.azure.com/eventstorage/eventstorage/_build/latest?definitionId=1&branchName=main)


### Overview

eventstorage is a high-performance event sourcing framework built for .Net that allows selecting event storage of choice. Combining consistency with schema flexibility, es aims to make event sourcing simplified for everyone. Currently supports Azure Sql, Postgres and Sql Server.

### Prerequisites

<!-- [![My Skills](https://skillicons.dev/icons?i=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->

eventstorage runs on the stable release of .Net 8 and requires the SDK installed.

    https://dotnet.microsoft.com/en-us/download/dotnet/8.0

<!-- [![My Skills](https://skillicons.dev/icons?i=docker)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->

Use docker to run sqlserver or postgres databases, execute `docker-compose`.

    docker compose --project-name eventstorage up -d

### Usage

<!-- [![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->


Simply install `EventStorage` package.

    dotnet add package EventStorage --prerelease

Use `AddEventStorage` service collection extension.

```csharp
var connectionString = builder.Configuration["postgresqlsecret"]??
    throw new Exception("No connection defined");

builder.Services.AddEventStorage(asynchandler =>
{
    asynchandler.Schema = "es";
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

To continue please visit our [GitHub](https://github.com/eventstorage/eventstorage) handle.

### Give us a star ⭐
If you are an event sourcer and love OSS, give [eventstorage](https://github.com/eventstorage/eventstorage) a star. :purple_heart:

### License

This project is licensed under the terms of [MIT](https://github.com/eventstorage/eventstorage/blob/main/LICENSE) license.
