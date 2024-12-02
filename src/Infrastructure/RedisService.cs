using EventStorage.Events;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;
using Redis.OM;
using Redis.OM.Searching;

namespace EventStorage.Infrastructure;

public class RedisService(IServiceProvider sp) : IRedisService
{
    private readonly RedisConnectionProvider _provider = sp.GetRequiredService<RedisConnectionProvider>();
    private IRedisCollection<Td> Collection<Td>() where Td : notnull => _provider.RedisCollection<Td>();
    public async Task<Td?> GetDocument<Td>(string sourceId) where Td : notnull =>
        await Collection<Td>().FindByIdAsync(sourceId);
    private async Task AddDocument<Td>(Td document) where Td : notnull =>
        await Collection<Td>().InsertAsync(document);
    private async Task AddDocument(object document) =>
        await _provider.Connection.SetAsync(document);
    public async Task RestoreProjections(
        EventSourceEnvelop source, IEnumerable<IProjection> projections, IProjectionRestorer restorer)
    {
        foreach (var projection in projections)
        {
            if(!restorer.Subscribes(source.SourcedEvents, projection))
                continue;
            var type = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
            var document = restorer.Project(projection, source.SourcedEvents, type)?? default!;
            await AddDocument(document);
        }
    }
}