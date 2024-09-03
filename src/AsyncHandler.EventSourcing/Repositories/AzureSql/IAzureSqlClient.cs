namespace AsyncHandler.EventSourcing.Repositories.AzureSql;

public interface IAzureSqlClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(long? sourceID = null);
    Task Commit(T aggregate);
}