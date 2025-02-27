using System.Reflection;
using EventStorage.Configurations;
using EventStorage.Projections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventStorage.Benchmarks;

public static class Container
{
    public static IServiceProvider ConfigureContainer(this IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets(Assembly.GetExecutingAssembly())
        .Build();

        services.AddEventStorage(eventstorage =>
        {
            eventstorage.AddEventSource(eventsource =>
            {
                eventsource.Schema = "es";
                eventsource.ConnectionString = configuration["mssqlsecret"];
                eventsource.Select(EventStore.SqlServer)
                .Project<OrderProjection>(ProjectionMode.Async)
                .Project<OrderDetailProjection>(ProjectionMode.Async)
                .Project<OrderDocumentProjection>(ProjectionMode.Async);
                // .Project<OrderDocumentProjection>(ProjectionMode.Async, src => src.Redis("redis://localhost"));
            });
        });
        return services.BuildServiceProvider();
    }
}