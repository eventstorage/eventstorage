using EventStorage.AggregateRoot;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Redis.OM;

namespace EventStorage.Configurations;

public class EventSourceConfiguration(IServiceCollection services, string schema) 
    : EventStorageConfiguration(services, schema)
{
    public EventStore Source { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public List<IProjection> Projections = [];

    // initialize stores while app spins up
    public void InitStore()
    {
        if(Projections.Any(x => x.Destination.Store == DestinationStore.Redis))
        {
            var p = Projections.First(x => x.Destination.Store == DestinationStore.Redis);
            ServiceCollection.AddSingleton(new RedisConnectionProvider(p.Destination.RedisConnection));
        }
        ServiceCollection.AddSingleton<IHostedService>((sp) =>
        {
            var repository = new Repository<IEventSource>(ConnectionString, sp, Source);
            var eventstorage = new EventStorage<IEventSource>(repository, Source);
            return new StoreInitializer(eventstorage, Projections, ServiceProvider);
        });
    }
}