using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories.AzureSql;

namespace AsyncHandler.EventSourcing.Repositories;

internal abstract class SourceBase<T>(string connectionString, IServiceProvider sp) where T : AggregateRoot
{
    public abstract EventSources Source { get; }
    public AzureSqlClient<T> AzureSqlClient => new (connectionString, sp);
    public AzureSqlClient<T> PostgreSqlClient => new (connectionString, sp);
    public AzureSqlClient<T> SqlServerClient => new (connectionString, sp);
    public abstract Task InitSource();
}