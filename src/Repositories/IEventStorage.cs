using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Repositories;

public interface IEventStorage<T>
{
    internal Task InitSource();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T t);
    Task<M?> Project<M>(string sourceId) where M : class;
    internal Task<Checkpoint> LoadCheckpoint();
    internal Task SaveCheckpoint(Checkpoint checkpoint);
    internal Task<IEnumerable<SourcedEvent>> LoadEventsPastSeq(long seq);
}