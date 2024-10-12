using EventStorage.Configurations;

namespace EventStorage.Repositories;

public class EventStorage<T>(IRepository<T> repository, EventSources source) : IEventStorage<T>
{
    public Task InitSource() => source switch
    {
        EventSources.PostgresSql => repository.PostgreSqlClient.Init(),
        _ => repository.SqlServerClient.Init(),
    };
    public Task<T> CreateOrRestore(string? sourceId = null) => source switch
    {
        EventSources.PostgresSql => repository.PostgreSqlClient.CreateOrRestore(sourceId),
        _ => repository.SqlServerClient.CreateOrRestore(sourceId),
    };
    
    public Task Commit(T t) => source switch
    {
        EventSources.PostgresSql => repository.PostgreSqlClient.Commit(t),
        _ => repository.SqlServerClient.Commit(t),
    };
}