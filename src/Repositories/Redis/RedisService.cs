using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;

namespace EventStorage.Repositories.Redis;

public class RedisService(RedisConnectionProvider redis, ILogger<RedisService> logger) : IRedisService
{
    // private readonly IRedisCollection<T> _collection =  redis.RedisCollection<T>();
    public async Task<T> GetDocument<T>(string sourceId) =>
        await Task.FromResult<T>(default!);
}