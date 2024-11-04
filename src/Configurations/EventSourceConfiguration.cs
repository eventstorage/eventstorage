using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Extensions;
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
    public void Init()
    {
        if(Projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
        {
            var p = Projections.First(x => x.Configuration.Store == ProjectionStore.Redis);
            ServiceCollection.AddSingleton(new RedisConnectionProvider(p.Configuration.ConnectionString));
        }
        ServiceCollection.AddSingleton<IHostedService>((sp) =>
        {
            var repository = new Repository<IEventSource>(ConnectionString, sp, Source);
            var eventstorage = new EventStorage<IEventSource>(repository, Source);
            return new StoreInitializer(eventstorage, Projections, ServiceProvider);
        });
        AddProjectionEngine();
    }
    private void AddProjectionEngine()
    {
        Dictionary<IProjection, List<MethodInfo>> projections = [];
        foreach (var projection in Projections)
        {
            var methods = projection.GetMethods();
            projections.Add(projection, methods.ToList());
        }
        ServiceCollection.AddSingleton<IProjectionEngine>(sp =>
        {
            return new ProjectionEngine(sp, projections);
        });
    }
}