namespace AsyncHandler.EventSourcing.Repositories.SqlServer;

public interface ISqlServerClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
}