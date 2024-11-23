using System.Reflection;
using EventStorage.Configurations;
using EventStorage.Extensions;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Repositories.PostgreSql;
using EventStorage.Repositories.SqlServer;
using EventStorage.Schema;
using EventStorage.Unit.Tests.AggregateRoot;
using EventStorage.Unit.Tests.Projections;
using EventStorage.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventStorage.Integration.Tests;

public class Configuration<T> where T : OrderAggregate
{
    public static readonly IServiceProvider Container = BuildContainer();
    private static readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddUserSecrets<Configuration<T>>()
        .AddEnvironmentVariables()
        .Build();
    private static ServiceProvider BuildContainer()
    {
        var services = new ServiceCollection();

        Dictionary<EventStore, IEventSourceSchema> schemas = [];
        schemas.Add(EventStore.AzureSql, new AzureSqlSchema("es"));
        schemas.Add(EventStore.PostgresSql, new PostgreSqlSchema("es"));
        schemas.Add(EventStore.SqlServer, new SqlServerSchema("es"));
        services.AddKeyedSingleton("Schema", schemas);

        services.AddSingleton<ILogger<SqlServerClient<T>>>(sp =>
            new Logger<SqlServerClient<T>>(new LoggerFactory()));
        services.AddSingleton<ILogger<PostgreSqlClient<T>>>(sp =>
            new Logger<PostgreSqlClient<T>>(new LoggerFactory()));

        foreach (EventStore source in Enum.GetValues(typeof(EventStore)))
        {
            services.AddKeyedSingleton<IRepository<T>>(source, (sp, o) =>
                new Repository<T>(GetConnection(source), sp, source));
            services.AddKeyedSingleton<IEventStorage<T>>(source, (sp, o) =>
                new EventStorage<T>(sp.GetRequiredKeyedService<IRepository<T>>(source), source));
        }

        OrderProjection projection = new(){ Mode = ProjectionMode.Transient, Configuration = new() };
        services.AddSingleton<IProjection>(projection);
        services.AddSingleton<IProjectionRestorer>(sp =>
        {
            Dictionary<IProjection, List<MethodInfo>> projections = [];
            projections.Add(projection, projection.GetMethods().ToList());
            return new ProjectionRestorer(sp, projections);
        });
        return services.BuildServiceProvider();
    }
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
}