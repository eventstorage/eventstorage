using EventStorage.AggregateRoot;
using EventStorage.Projections;
using EventStorage.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Redis.OM;

namespace EventStorage.Workers;

internal class StoreInitializer(
    IEventStorage<IEventSource> eventStorage, IEnumerable<IProjection> projections,
    IServiceProvider sp) : IHostedService
{
    private readonly RedisConnectionProvider _redis = sp.GetRequiredService<RedisConnectionProvider>();
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await eventStorage.InitSource();

        var redisTypes = projections.Where(x => x.Destination.Store == DestinationStore.Redis)
            .Select(x => x.GetType().BaseType?.GenericTypeArguments.First());
        foreach (var item in redisTypes)
        {
            await _redis.Connection.CreateIndexAsync(item?? default!);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
