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
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await eventStorage.InitSource();

        if(projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
        {
            RedisConnectionProvider _redis = sp.GetRequiredService<RedisConnectionProvider>();
            projections.Where(x => x.Configuration.Store == ProjectionStore.Redis)
                .Select(x => x.GetType().BaseType?.GenericTypeArguments.First()).ToList()
                .ForEach(async x => await _redis.Connection.CreateIndexAsync(x?? default!));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
