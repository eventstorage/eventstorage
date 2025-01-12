using EventStorage.Events;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Infrastructure;

public interface IEventStorage<T>
{
    Task InitSource();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T t);
    Task<M?> Project<M>(string sourceId) where M : class;
    internal Task<EventSourceEnvelop> LoadEventSource(long sourceId);
    internal Task<Checkpoint> LoadCheckpoint();
    internal Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false);
    internal Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c);
    internal Task RestoreProjection(Projection projection, IEnumerable<EventEnvelop> events, IServiceScopeFactory scope);
}