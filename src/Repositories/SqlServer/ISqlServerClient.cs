using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Repositories.SqlServer;

public interface ISqlServerClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
    Task<M?> Project<M>(string sourceId) where M : class;
    Task<Checkpoint> LoadCheckpoint();
    Task SaveCheckpoint(Checkpoint checkpoint);
    Task<IEnumerable<SourcedEvent>> LoadEventsPastCheckpoint(Checkpoint c);
}