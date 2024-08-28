using AsyncHandler.EventSourcing.Configuration;

namespace AsyncHandler.EventSourcing.Repositories;

internal class EventSource<T>(string connectionString, IServiceProvider sp, EventSources eventSource) 
    : SourceBase<T>(connectionString, sp), IEventSource<T> where T : AggregateRoot
{
    public override EventSources Source  => eventSource;
    public override Task InitSource() => Source switch
    {
        EventSources.AzureSql => AzureSqlClient.InitAzureSql(),
        EventSources.PostgresSql => PostgreSqlClient.InitAzureSql(),
        EventSources.SQLServer => SqlServerClient.InitAzureSql(),
        _ => Task.CompletedTask,
    };
    public Task<T> CreateOrRestore(string sourceId) => Source switch
    {
        EventSources.AzureSql => AzureSqlClient.CreateOrRestore(sourceId),
        EventSources.PostgresSql => PostgreSqlClient.CreateOrRestore(sourceId),
        EventSources.SQLServer => SqlServerClient.CreateOrRestore(sourceId),
        _ => AzureSqlClient.CreateOrRestore(sourceId),
    };
    
    public Task Commit(T t) => Source switch
    {
        EventSources.AzureSql => AzureSqlClient.Commit(t),
        EventSources.PostgresSql => PostgreSqlClient.Commit(t),
        EventSources.SQLServer => SqlServerClient.Commit(t),
        _ => Task.CompletedTask,
    };
    
}