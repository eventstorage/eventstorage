namespace AsyncHandler.EventSourcing.SourceRepositories.AzureSql;

public interface IAzureSqlClient<T>
{
    Task<T> CreateOrRestore(string sourceId);
    Task Commit(T t);
}