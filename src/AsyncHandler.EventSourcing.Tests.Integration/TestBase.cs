using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories;
using AsyncHandler.EventSourcing.Repositories.AzureSql;
using AsyncHandler.EventSourcing.Schema;
using AsyncHandler.EventSourcing.Tests.Unit;
using Castle.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Tests.Integration;

public abstract class TestBase
{
    private static string GetConnection(EventSources source) => BuildConfiguration(source);
    public static IEventSource<OrderAggregate> GetEventSource(EventSources source) =>
        BuildContainer(source).GetRequiredService<IEventSource<OrderAggregate>>();
    public static ServiceProvider BuildContainer(EventSources source)
    {
        var services = new ServiceCollection();
        Dictionary<EventSources,IEventSourceSchema> schemas = [];
        schemas.Add(EventSources.AzureSql, new AzureSqlSchema());
        schemas.Add(EventSources.PostgresSql, new PostgreSqlSchema());
        schemas.Add(EventSources.SqlServer, new SqlServerSchema());
        services.AddKeyedSingleton("Schema", schemas);
        services.AddTransient<ILogger<AzureSqlClient<OrderAggregate>>>(sp =>
        {
            return new Logger<AzureSqlClient<OrderAggregate>>(new LoggerFactory());
        });
        services.AddSingleton<IRepository<OrderAggregate>>(sp =>
        {
            return new Repository<OrderAggregate>(GetConnection(source), sp, source);
        });
        services.AddSingleton<IEventSource<OrderAggregate>>(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<OrderAggregate>>();
            return new EventSource<OrderAggregate>(repository, source);
        });
        return services.BuildServiceProvider();
    }
    protected static string BuildConfiguration(EventSources source)
    {
        var builder = new ConfigurationBuilder()
        .AddUserSecrets<TestBase>().AddEnvironmentVariables();
        return source switch
        {
            EventSources.SqlServer => builder.Build().GetValue<string>("mssqlsecret") ??
                throw new Exception("no connection string found"),
            EventSources.AzureSql => builder.Build().GetValue<string>("azuresqlsecret") ??
                throw new Exception("no connection string found"),
            EventSources.PostgresSql => builder.Build().GetValue<string>("mssqlsecret") ??
                throw new Exception("no connection string found"),
            _ => string.Empty
        };
    }
}