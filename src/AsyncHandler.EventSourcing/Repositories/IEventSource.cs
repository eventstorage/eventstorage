namespace AsyncHandler.EventSourcing.Repositories;

public interface IEventSource<T>
{
    Task? CreateOrRestore(string sourceId);
    Task? Commit(T source);
}