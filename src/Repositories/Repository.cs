using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories.PostgreSql;
using AsyncHandler.EventSourcing.Repositories.SqlServer;

namespace AsyncHandler.EventSourcing.Repositories;

public class Repository<T>(string conn, IServiceProvider sp, EventSources source) 
    : IRepository<T> where T : IAggregateRoot
{
    private readonly SqlServerClient<T> _sqlServer = new(conn, sp, source);
    private readonly PostgreSqlClient<T> _postgres = new(conn, sp);
    public IPostgreSqlClient<T> PostgreSqlClient => _postgres;
    public ISqlServerClient<T> SqlServerClient => _sqlServer;
}