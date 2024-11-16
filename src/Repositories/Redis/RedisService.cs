using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace EventStorage.Repositories.Redis;

public class RedisService(IServiceProvider sp) : IRedisService
{
    public async Task<T> GetDocument<T>(string sourceId) =>
        await Task.FromResult<T>(default!);
    public async Task StoreDocument<T>(T document) where T : notnull
    {
        IRedisCollection<T> _collection =  sp.GetRequiredService<RedisConnectionProvider>()
            .RedisCollection<T>();
        await Task.CompletedTask;
    }
    public async Task RestoreProjections(EventSourceEnvelop source)
    {
        await Task.CompletedTask;
    }
}