using EventStorage.Repositories.PostgreSql;
using EventStorage.Repositories.SqlServer;

namespace EventStorage.Repositories;

public interface IRepository<T> 
{
    IPostgreSqlClient<T> PostgreSqlClient { get; }
    ISqlServerClient<T> SqlServerClient { get; }
}