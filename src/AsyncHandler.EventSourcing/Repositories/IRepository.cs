using AsyncHandler.EventSourcing.Repositories.AzureSql;

public interface IRepository<T>
{
    public IAzureSqlClient<T> AzureSqlClient { get; }
    public IAzureSqlClient<T> SqlServerClient { get; }
    public IAzureSqlClient<T> PostgreSqlClient { get; }
}