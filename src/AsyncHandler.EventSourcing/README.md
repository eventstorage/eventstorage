# asynchandler

### A lightweight event sourcing framework with event-store of choice.

[![Github follow](https://img.shields.io/badge/follow-asynchandler-bf9136?logo=github)](https://github.com/asynchandler)
[![Github follow](https://img.shields.io/badge/follow-eventsourcer-bf9136?logo=github)](https://github.com/eventsourcer)
[![In follow](https://img.shields.io/badge/follow-LinkedIn-blue?logo=linkedin)](https://www.linkedin.com/in/sarwansurchi/)
[![Nuget Package](https://badgen.net/nuget/v/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Nuget](https://badgen.net/nuget/dt/asynchandler.eventsourcing)](https://www.nuget.org/packages/AsyncHandler.EventSourcing)
[![Github follow](https://img.shields.io/badge/give_us_a-⭐-yellow?logo=github)](https://github.com/asynchandler/AsyncHandler.EventSourcing)
[![build Status](https://dev.azure.com/asynchandler/AsyncHandler.EventSourcing/_apis/build/status%2Fasynchandler.AsyncHandler.EventSourcing?branchName=main&label=azure%20pipes)](https://dev.azure.com/asynchandler/AsyncHandler.EventSourcing/_build/latest?definitionId=11&branchName=main)


<!-- <div align="left">
    <img src="../../assets/ah_radius.png" width="80" height="80" style="float:left;" alt="asynchandler">
</div> -->

### Overview

asynchandler is a high-performance event sourcing framework. combining consistency with schema flexibility, asynchandler aims to simplify event sourcing for everyone. Currently supports Azure Sql database and Sql Server, with Postgres in the upcoming releases.

### Prerequisites

[![My Skills](https://skillicons.dev/icons?i=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

asynchandler runs on the stable release of .Net 8 and requires the SDK installed.

    https://dotnet.microsoft.com/en-us/download/dotnet/8.0

[![My Skills](https://skillicons.dev/icons?i=docker)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

Use docker to run sqlserver or postgres databases, execute `docker-compose`.

    docker compose --project-name some-name up -d

### Usage

[![My Skills](https://skillicons.dev/icons?i=vscode)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
##### Install the package

Simply install `AsyncHandler.EventSourcing` package.

    dotnet add package AsyncHandler.EventSourcing

##### Use `AddAsyncHandler` service collection extension

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

To continue please visit our [GitHub](https://github.com/asynchandler/AsyncHandler.EventSourcing) space.

### Give us a star ⭐
If you are an event sourcer and love OSS, give [us](https://github.com/asynchandler/AsyncHandler.EventSourcing) a star. :purple_heart:

### License

This project is licensed under the terms of the [MIT](https://github.com/asynchandler/AsyncHandler.EventSourcing/blob/main/LICENSE) license.
