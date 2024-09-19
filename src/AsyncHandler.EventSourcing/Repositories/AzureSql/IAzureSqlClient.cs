namespace AsyncHandler.EventSourcing.Repositories.AzureSql;

public interface IAzureSqlClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
}