namespace AsyncHandler.EventSourcing.Repositories.AzureSql;

public interface IAzureSqlClient<T>
{
    Task<T> CreateOrRestore(string sourceId);
    Task Commit(T t);
}