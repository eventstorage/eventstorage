namespace EventStorage.Repositories;

public interface IEventStorage<T>
{
    Task InitSource();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T t);
    Task<M> Project<M>(string sourceId) where M : class;
}