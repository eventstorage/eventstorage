namespace AsyncHandler.EventSourcing.Repositories;

public interface IEventSource<T>
{
    Task<T> CreateOrRestore(string sourceId);
    Task Commit(T source);
}