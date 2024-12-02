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
        Console.WriteLine(configuration["postgresqlsecret"]);

        services.AddEventStorage(eventstorage =>
        {
            eventstorage.AddEventSource(eventsource =>
            {
                eventsource.Schema = "es";
                eventstorage.ConnectionString = configuration["postgresqlsecret"];
                eventsource.Select(EventStore.PostgresSql)
                .Project<OrderProjection>(ProjectionMode.Transient)
                .Project<OrderDetailProjection>(ProjectionMode.Async)
                .Project<OrderDocumentProjection>(ProjectionMode.Async, src => src.Redis("redis://localhost"));
            });
        });

        return services.BuildServiceProvider();
    }
}