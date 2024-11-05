namespace EventStorage.Repositories.Redis;

public interface IRedisService
{
    Task<T> GetDocument<T>(string sourceId);
}