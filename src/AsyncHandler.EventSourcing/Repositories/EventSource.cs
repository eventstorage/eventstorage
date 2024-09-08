using AsyncHandler.EventSourcing.Configuration;

namespace AsyncHandler.EventSourcing.Repositories;

public class EventSource<T>(IRepository<T> repository, EventSources source) 
    : IEventSource<T> where T : AggregateRoot
{
    public Task InitSource() => source switch
    {
        EventSources.AzureSql => repository.AzureSqlClient.Init(),
        EventSources.PostgresSql => repository.PostgreSqlClient.Init(),
        EventSources.SqlServer => repository.SqlServerClient.Init(),
        _ => Task.CompletedTask,
    };
    public Task<T> CreateOrRestore(long? sourceId = null) => source switch
    {
        EventSources.AzureSql => repository.AzureSqlClient.CreateOrRestore(sourceId),
        EventSources.PostgresSql => repository.PostgreSqlClient.CreateOrRestore(sourceId),
        EventSources.SqlServer => repository.SqlServerClient.CreateOrRestore(sourceId),
        _ => repository.AzureSqlClient.CreateOrRestore(sourceId),
    };
    
    public Task Commit(T t) => source switch
    {
        EventSources.AzureSql => repository.AzureSqlClient.Commit(t),
        EventSources.PostgresSql => repository.PostgreSqlClient.Commit(t),
        EventSources.SqlServer => repository.SqlServerClient.Commit(t),
        _ => Task.CompletedTask,
    };
}