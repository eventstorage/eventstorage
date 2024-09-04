using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories.AzureSql;

namespace AsyncHandler.EventSourcing.Repositories;

public class Repository<T>(string conn, IServiceProvider sp, EventSources source)
    : IRepository<T> where T : AggregateRoot
{
    private AzureSqlClient<T>? _azureSql;
    private AzureSqlClient<T>? _sqlServer;
    private AzureSqlClient<T>? _postgres;
    public IAzureSqlClient<T> AzureSqlClient => _azureSql ??= new AzureSqlClient<T>(conn, sp, source);
    public IAzureSqlClient<T> PostgreSqlClient => _postgres ??= new AzureSqlClient<T>(conn, sp, source);
    public IAzureSqlClient<T> SqlServerClient => _sqlServer ??= new AzureSqlClient<T>(conn, sp, source);
}