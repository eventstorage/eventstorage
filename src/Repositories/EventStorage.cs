using EventStorage.Configurations;
using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Repositories;

public class EventStorage<T>(IRepository<T> repository, EventStore source) : IEventStorage<T>
{
    public Task InitSource() => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.Init(),
        _ => repository.SqlServerClient.Init(),
    };
    public Task<T> CreateOrRestore(string? sourceId = null) => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.CreateOrRestore(sourceId),
        _ => repository.SqlServerClient.CreateOrRestore(sourceId),
    };
    
    public Task Commit(T t) => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.Commit(t),
        _ => repository.SqlServerClient.Commit(t),
    };
    public Task<M?> Project<M>(string sourceId) where M : class => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.Project<M>(sourceId),
        _ => repository.SqlServerClient.Project<M>(sourceId)
    };
    public Task<Checkpoint> LoadCheckpoint() => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.LoadCheckpoint(),
        _ => repository.SqlServerClient.LoadCheckpoint()
    };
    public Task SaveCheckpoint(Checkpoint checkpoint) => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.SaveCheckpoint(checkpoint),
        _ => repository.SqlServerClient.SaveCheckpoint(checkpoint)
    };
    public Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c) => source switch
    {
        EventStore.PostgresSql => repository.PostgreSqlClient.LoadEventsPastCheckpoint(c),
        _ => repository.SqlServerClient.LoadEventsPastCheckpoint(c)
    };
}