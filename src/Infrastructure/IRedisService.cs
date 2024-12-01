using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Infrastructure;

public interface IRedisService<T> where T : IEventSource
{
    Task<Td?> GetDocument<Td>(string sourceId) where Td : notnull;
    Task RestoreProjections(EventSourceEnvelop source, IEnumerable<IProjection<T>> projections, IProjectionRestorer<T> restorer);
}