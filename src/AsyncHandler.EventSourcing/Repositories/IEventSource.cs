using AsyncHandler.EventSourcing.Repositories.AzureSql;

public interface IEventSource<T>
{
    Task<T> CreateOrRestore(string sourceId);
    Task Commit(T t);
}