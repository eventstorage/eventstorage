using AsyncHandler.EventSourcing.Repositories.AzureSql;

namespace AsyncHandler.EventSourcing.Repositories;

public interface IRepository<T> where T : AggregateRoot
{
    public AzureSqlClient<T> AzureSqlClient { get; }
    public AzureSqlClient<T> PostgreSqlClient { get; }
    public AzureSqlClient<T> SqlServerClient { get; }
}