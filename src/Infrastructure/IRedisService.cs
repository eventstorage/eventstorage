using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Infrastructure;

public interface IRedisService
{
    Task<Td?> GetDocument<Td>(string sourceId) where Td : notnull;
    Task RestoreProjection(IProjection projection, IEnumerable<SourcedEvent> events);
}