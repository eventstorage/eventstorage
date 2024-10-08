namespace EventStorage.Repositories;

public interface IEventSource<T>
{
    Task InitSource();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T t);
}