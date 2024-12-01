using System.Reflection;
using EventStorage.Configurations;
using EventStorage.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Benchmarks;

public static class Container
{
    public static IServiceProvider Build()
    {
        var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets(Assembly.GetExecutingAssembly())
        .Build();

        var services = new ServiceCollection();

        services.AddEventStorage(eventstorage =>
        {
            eventstorage.AddEventSource<OrderBookingLong>(eventsource =>
            {
                eventsource.Schema = "longschema";
                eventsource.Select(EventStore.PostgresSql, configuration["postgresqlsecret"]?? "")
                .Project<OrderProjection>(ProjectionMode.Async);
            });
            eventstorage.AddEventSource<OrderBookingGuid>(eventsource =>
            {
                eventsource.Schema = "guidschema";
                eventsource.Select(EventStore.PostgresSql, configuration["postgresqlsecret"]?? "")
                .Project<OrderDetailProjection>(ProjectionMode.Async);
            });
        });

        return services.BuildServiceProvider();
    }
}