# eventstorage

### A lightweight event sourcing framework for .NET with event storage of choice.

[![Github follow](https://img.shields.io/badge/follow-eventstorage-bf9136?logo=github)](https://github.com/eventstorage)
[![Nuget Package](https://badgen.net/nuget/v/eventstorage)](https://www.nuget.org/packages/eventstorage)
[![Nuget](https://badgen.net/nuget/dt/eventstorage)](https://www.nuget.org/packages/eventstorage)
[![Github follow](https://img.shields.io/badge/give_us_a-⭐-yellow?logo=github)](https://github.com/eventstorage/eventstorage)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![build Status](https://dev.azure.com/eventstorage/eventstorage/_apis/build/status%2Feventstorage?branchName=main&label=azure%20pipes)](https://dev.azure.com/eventstorage/eventstorage/_build/latest?definitionId=1&branchName=main)


### Overview

eventstorage is a high-performance framework powered by latest innovative C# that allows selecting event storage of choice with Azure Sql, Postgres and Sql Server, and offers multiple projection modes with support to Redis for high-performance asynchronous projections.

es denormalizes and uses a flexible schema, running everything through plain sql resulting in lightning-fast storage operations.

### Prerequisites

<!-- [![My Skills](https://skillicons.dev/icons?i=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->

eventstorage runs on the stable release of .NET 8 and requires the SDK installed.

    https://dotnet.microsoft.com/en-us/download/dotnet/8.0

<!-- [![My Skills](https://skillicons.dev/icons?i=docker)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->

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

### Usage

<!-- [![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) -->


Simply install `EventStorage` package.

    dotnet add package EventStorage --prerelease

Use `AddEventStorage` service collection extension.

```csharp
builder.Services.AddEventStorage(eventstorage =>
{
    eventstorage.Schema = "es";
    eventstorage.AddEventSource(source =>
    {
        eventsource.Select(EventStore.PostgresSql, connectionString)
        .Project<OrderProjection>(ProjectionMode.Consistent)
        .Project<OrderDocumentProjection>(ProjectionMode.Async, source => source.Redis("redis://localhost"));
    });
});
```
Use configuration options to select event storage of choice and make sure connection string is defined.

#### Create aggregate root
Add your aggregate with `EventSource<TId>`

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
```
`EventSource<TId>` allows selecting `long` or `Guid` to identify event streams.
While selecting `long` offers lightnening-fast queries and is human readable, we can always switch back and forth between the two!

To continue please visit our [GitHub](https://github.com/eventstorage/eventstorage) or our beautiful [documentation](https://eventstorage.github.io) site.

### Give us a star ⭐
If you are an event sourcer and love OSS, give [eventstorage](https://github.com/eventstorage/eventstorage) a star. :purple_heart:

### License

This project is licensed under the terms of [MIT](https://github.com/eventstorage/eventstorage/blob/main/LICENSE) license.
