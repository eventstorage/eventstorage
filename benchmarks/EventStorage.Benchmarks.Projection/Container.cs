using EventStorage.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Benchmarks.Projections;

public static class Container
{
    public static IServiceProvider Build()
    {
        // var configuration = new ConfigurationBuilder()
        // .AddEnvironmentVariables()
        // .AddUserSecrets(typeof(Program).Assembly)
        // .Build();

        var services = new ServiceCollection();

        services.AddEventStorage(eventstorage =>
        {
            eventstorage.AddEventSource(eventsource =>
            {
                eventsource.Project<OrderProjection>(ProjectionMode.Consistent)
                .Project<OrderDetailProjection>(ProjectionMode.Consistent);
            });
        });

        return services.BuildServiceProvider();
    }
}