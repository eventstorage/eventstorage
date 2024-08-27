using AsyncHandler.EventSourcing.Configuration;

namespace AsyncHandler.EventSourcing.Repositories;

internal class EventSource<T>(
    IRepository<T> repository,
    EventSources source) : IEventSource<T> where T : AggregateRoot
{
    public Task<T> CreateOrRestore(string sourceId)
    {
        return source switch
        {
            EventSources.AzureSql => repository.AzureSqlClient.CreateOrRestore(sourceId),
            EventSources.PostgresSql => repository.PostgreSqlClient.CreateOrRestore(sourceId),
            EventSources.SQLServer => repository.SqlServerClient.CreateOrRestore(sourceId),
            _ => repository.AzureSqlClient.CreateOrRestore(sourceId),
        };
    }
    public Task Commit(T t)
    {
        return source switch
        {
            EventSources.AzureSql => repository.AzureSqlClient.Commit(t),
            EventSources.PostgresSql => repository.PostgreSqlClient.Commit(t),
            EventSources.SQLServer => repository.SqlServerClient.Commit(t),
            _ => Task.CompletedTask,
        };
    }
}