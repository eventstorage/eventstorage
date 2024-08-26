using AsyncHandler.EventSourcing.Configuration;

namespace AsyncHandler.EventSourcing.Repositories;

public class EventSource<T>(IRepository<T> repository) : IEventSource<T> where T : AggregateRoot
{
    public EventSources Source { get; set; }
    public Task? CreateOrRestore(string sourceId)
    {
        return Source switch
        {
            EventSources.AzureSql => repository.AzureSqlClient?.CreateOrRestore(sourceId),
            EventSources.PostgresSql => repository.PostgreSqlClient?.CreateOrRestore(sourceId),
            EventSources.SQLServer => repository.SqlServerClient?.CreateOrRestore(sourceId),
            _ => Task.CompletedTask,
        };
    }
    public Task? Commit(T t)
    {
        return Source switch
        {
            EventSources.AzureSql => repository.AzureSqlClient?.Commit(t),
            EventSources.PostgresSql => repository.PostgreSqlClient?.Commit(t),
            EventSources.SQLServer => repository.SqlServerClient?.Commit(t),
            _ => Task.CompletedTask,
        };
    }
}