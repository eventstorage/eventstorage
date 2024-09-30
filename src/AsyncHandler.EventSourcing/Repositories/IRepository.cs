using AsyncHandler.EventSourcing.Repositories.PostgreSql;
using AsyncHandler.EventSourcing.Repositories.SqlServer;

namespace AsyncHandler.EventSourcing.Repositories;

public interface IRepository<T> 
{
    IPostgreSqlClient<T> PostgreSqlClient { get; }
    ISqlServerClient<T> SqlServerClient { get; }
}