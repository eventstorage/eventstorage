using AsyncHandler.EventSourcing.Repositories.AzureSql;

namespace AsyncHandler.EventSourcing.Repositories;

public interface IRepository<T> 
{
    IAzureSqlClient<T> AzureSqlClient { get; }
    IAzureSqlClient<T> PostgreSqlClient { get; }
    IAzureSqlClient<T> SqlServerClient { get; }
}