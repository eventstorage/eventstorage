using AsyncHandler.EventSourcing.Configuration;

namespace AsyncHandler.EventSourcing.Repositories;

public class EventSource<T>(IRepository<T> repository, EventSources eventSource) 
    : IEventSource<T> where T : AggregateRoot
{
    public EventSources Source  => eventSource;
    public Task InitSource() => Source switch
    {
        EventSources.AzureSql => repository.AzureSqlClient.Init(),
        EventSources.PostgresSql => repository.PostgreSqlClient.Init(),
        EventSources.SQLServer => repository.SqlServerClient.Init(),
        _ => Task.CompletedTask,
    };
    public Task<T> CreateOrRestore(long? sourceId = null) => Source switch
    {
        EventSources.AzureSql => repository.AzureSqlClient.CreateOrRestore(sourceId),
        EventSources.PostgresSql => repository.PostgreSqlClient.CreateOrRestore(sourceId),
        EventSources.SQLServer => repository.SqlServerClient.CreateOrRestore(sourceId),
        _ => repository.AzureSqlClient.CreateOrRestore(sourceId),
    };
    
    public Task Commit(T t) => Source switch
    {
        EventSources.AzureSql => repository.AzureSqlClient.Commit(t),
        EventSources.PostgresSql => repository.PostgreSqlClient.Commit(t),
        EventSources.SQLServer => repository.SqlServerClient.Commit(t),
        _ => Task.CompletedTask,
    };
}