using EventStorage.Events;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Repositories.SqlServer;

public interface ISqlServerClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
    Task<M?> Project<M>(string sourceId) where M : class;
    Task<Checkpoint> LoadCheckpoint();
    Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false);
    Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c);
    Task<long> RestoreProjections(EventSourceEnvelop source, IServiceScopeFactory scope);
    Task<IEnumerable<EventEnvelop>> LoadEventSource(long sourceId);
}