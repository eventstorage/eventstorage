using AsyncHandler.EventSourcing.Repositories.AzureSql;

public interface IEventSource<T>
{
    Task<T> CreateOrRestore(long? sourceId = null);
    Task Commit(T t);
}