using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Infrastructure;

public interface IRedisService
{
    Task<T?> GetDocument<T>(string sourceId) where T : notnull;
    Task RestoreProjections(EventSourceEnvelop source, IEnumerable<IProjection> projections, IProjectionRestorer restorer);
}