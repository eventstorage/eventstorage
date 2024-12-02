using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Infrastructure;

public interface IRedisService
{
    Task<Td?> GetDocument<Td>(string sourceId) where Td : notnull;
    Task RestoreProjections(EventSourceEnvelop source, IEnumerable<IProjection> projections, IProjectionRestorer restorer);
}