using EventStorage.Events;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Repositories.PostgreSql;

public interface IPostgreSqlClient<T>
{
    Task Init();
    Task<T> CreateOrRestore(string? sourceId = null);
    Task Commit(T aggregate);
    Task<M?> Project<M>(string sourceId);
    Task<Checkpoint> LoadCheckpoint();
    Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false);
    Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c);
    Task RestoreProjections(EventSourceEnvelop source, IServiceScopeFactory scope);
}