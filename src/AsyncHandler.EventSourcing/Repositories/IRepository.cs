using AsyncHandler.EventSourcing.Repositories.AzureSql;

namespace AsyncHandler.EventSourcing.Repositories;

public interface IRepository<T> where T : AggregateRoot
{
    public IAzureSqlClient<T> AzureSqlClient { get; }
    public IAzureSqlClient<T> PostgreSqlClient { get; }
    public IAzureSqlClient<T> SqlServerClient { get; }
}