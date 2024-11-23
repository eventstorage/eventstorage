using System.Text.Json;
using EventStorage.Events;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace EventStorage.Repositories.Redis;

public class RedisService(IServiceProvider sp) : IRedisService
{
    private readonly RedisConnectionProvider _provider = sp.GetRequiredService<RedisConnectionProvider>();
    private IRedisCollection<T> Collection<T>() where T : notnull => _provider.RedisCollection<T>();
    public async Task<T?> GetDocument<T>(string sourceId) where T : notnull =>
        await Collection<T>().FindByIdAsync(sourceId);
    private async Task AddDocument<T>(T document) where T : notnull =>
        await Collection<T>().InsertAsync(document);
    private async Task AddDocument(object document) =>
        await _provider.Connection.SetAsync(document);
    public async Task RestoreProjections(
        EventSourceEnvelop source,
        IEnumerable<IProjection> projections,
        IProjectionRestorer restorer)
    {
        foreach (var projection in projections)
        {
            if(!restorer.Subscribes(source.SourcedEvents, projection))
                continue;
            var type = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
            var document = restorer.Project(projection, source.SourcedEvents, type);
            await AddDocument(document);
        }
    }
}