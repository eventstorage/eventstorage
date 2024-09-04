using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories;
using AsyncHandler.EventSourcing.Repositories.AzureSql;
using AsyncHandler.EventSourcing.Tests.Unit;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public abstract class TestBase
{
    private static readonly string _connection = Configuration["TestAzureSqlDatabase"] ??"";
    private static IConfiguration Configuration =>
        new ConfigurationBuilder().AddUserSecrets<TestBase>().Build();
    public static IEventSource<OrderAggregate> GetEventSource(EventSources source) =>
        BuildContainer(source).GetRequiredService<IEventSource<OrderAggregate>>();
    public static ServiceProvider BuildContainer(EventSources source)
    {
        var services = new ServiceCollection();
        services.AddTransient<ILogger<AzureSqlClient<OrderAggregate>>>(sp =>
        {
            return new Logger<AzureSqlClient<OrderAggregate>>(new LoggerFactory());
        });
        services.AddSingleton<IRepository<OrderAggregate>>(sp =>
        {
            return new Repository<OrderAggregate>(_connection, sp);
        });
        services.AddSingleton<IEventSource<OrderAggregate>>(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<OrderAggregate>>();
            return new EventSource<OrderAggregate>(repository, source);
        });
        return services.BuildServiceProvider();
    }
}