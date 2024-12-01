using EventStorage.Infrastructure;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Redis.OM;

namespace EventStorage.Workers;

internal class StoreInitializer<T>(
    IServiceProvider sp, IEnumerable<Projection> projections) : IHostedService
{
    private readonly IEventStorage<T> _storage = sp.CreateScope().ServiceProvider.GetRequiredService<IEventStorage<T>>();
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _storage.InitSource();

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
