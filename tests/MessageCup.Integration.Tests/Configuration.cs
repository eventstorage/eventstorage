using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories;
using AsyncHandler.EventSourcing.Repositories.PostgreSql;
using AsyncHandler.EventSourcing.Repositories.SqlServer;
using AsyncHandler.EventSourcing.Schema;
using AsyncHandler.EventSourcing.Tests.Unit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Tests.Integration;

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
        
        Dictionary<EventSources,IEventSourceSchema> schemas = [];
        schemas.Add(EventSources.AzureSql, new AzureSqlSchema("ah"));
        schemas.Add(EventSources.PostgresSql, new PostgreSqlSchema("ah"));
        schemas.Add(EventSources.SqlServer, new SqlServerSchema("ah"));
        services.AddKeyedSingleton("Schema", schemas);

        services.AddSingleton<ILogger<SqlServerClient<T>>>(sp =>
            new Logger<SqlServerClient<T>>(new LoggerFactory()));
        services.AddSingleton<ILogger<PostgreSqlClient<T>>>(sp =>
            new Logger<PostgreSqlClient<T>>(new LoggerFactory()));

        foreach(EventSources source in Enum.GetValues(typeof(EventSources)))
        {
            services.AddKeyedSingleton<IRepository<T>>(source, (sp, o) =>
                new Repository<T>(GetConnection(source), sp, source));
            services.AddKeyedSingleton<IEventSource<T>>(source, (sp, o) =>
                new EventSource<T>(sp.GetRequiredKeyedService<IRepository<T>>(source), source));
        }
        return services.BuildServiceProvider();
    }
    private static string GetConnection(EventSources source) => source switch
        {
            EventSources.SqlServer => _configuration["mssqlsecret"]??
                throw new Exception("no connection string found"),
            EventSources.AzureSql => _configuration["azuresqlsecret"]??
                throw new Exception("no connection string found"),
            EventSources.PostgresSql => _configuration["postgresqlsecret"]??
                throw new Exception("no connection string found"),
            _ => string.Empty
        };
}