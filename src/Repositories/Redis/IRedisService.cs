using EventStorage.Events;

namespace EventStorage.Repositories.Redis;

public interface IRedisService
{
    Task<T> GetDocument<T>(string sourceId);
    Task StoreDocument<T>(T document) where T : notnull;
    Task RestoreProjections(EventSourceEnvelop source);
}