namespace EventStorage.Repositories.PostgreSql;

public interface IPostgreSqlClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
    Task<M?> Project<M>(string sourceId);
    Task<long> LoadCheckpoint();
    Task<bool> SaveCheckpoint();
}