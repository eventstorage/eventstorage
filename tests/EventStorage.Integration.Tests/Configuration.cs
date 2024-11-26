using EventStorage.Configurations;
using EventStorage.Projections;
using EventStorage.Unit.Tests.AggregateRoot;
using EventStorage.Unit.Tests.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Integration.Tests;

public class Configuration<T> where T : OrderAggregate
{
    public static IServiceProvider Container(EventStore store) => BuildContainer(store);
    private static readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddUserSecrets<Configuration<T>>()
        .AddEnvironmentVariables()
        .Build();
    private static string GetConnection(EventStore source) => source switch
    {
        EventStore.SqlServer => _configuration["mssqlsecret"] ??
            throw new Exception("no connection string found"),
        EventStore.AzureSql => _configuration["azuresqlsecret"] ??
            throw new Exception("no connection string found"),
        EventStore.PostgresSql => _configuration["postgresqlsecret"] ??
            throw new Exception("no connection string found"),
        _ => string.Empty
    };
    private static ServiceProvider BuildContainer(EventStore store) =>  new ServiceCollection()
    .AddEventStorage(storage =>
    {
        storage.AddEventSource(eventSource =>
        {
            eventSource.Select(store, GetConnection(store))
            .Project<OrderProjection>(ProjectionMode.Transient);
        });
    }).BuildServiceProvider();
}