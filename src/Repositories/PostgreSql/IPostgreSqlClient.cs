using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Repositories.PostgreSql;

public interface IPostgreSqlClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
    Task<M?> Project<M>(string sourceId);
    Task<Checkpoint> LoadCheckpoint();
    Task SaveCheckpoint(Checkpoint checkpoint);
    Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c);
    Task RestoreProjections(EventSourceEnvelop source);
}