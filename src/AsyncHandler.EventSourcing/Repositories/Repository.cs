using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories.AzureSql;

namespace AsyncHandler.EventSourcing.Repositories;

public class Repository<T>(string conn, IServiceProvider sp, EventSources source) 
    : IRepository<T> where T : IAggregateRoot
{
    private readonly AzureSqlClient<T> _azureSql = new(conn, sp, source);
    private readonly AzureSqlClient<T> _sqlServer = new(conn, sp, source);
    private readonly AzureSqlClient<T> _postgres = new(conn, sp, source);
    public IAzureSqlClient<T> AzureSqlClient => _azureSql;
    public IAzureSqlClient<T> PostgreSqlClient => _postgres;
    public IAzureSqlClient<T> SqlServerClient => _sqlServer;
}