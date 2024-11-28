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
            eventstorage.AddEventSource(eventsource =>
            {
                eventsource.Select(EventStore.PostgresSql, configuration["postgresqlsecret"]?? "")
                .Project<OrderProjection>(ProjectionMode.Async);
            });
        });

        return services.BuildServiceProvider();
    }
}