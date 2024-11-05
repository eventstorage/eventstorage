namespace EventStorage.Repositories.SqlServer;

public interface ISqlServerClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
    Task<M?> Project<M>(string sourceId) where M : class;
}