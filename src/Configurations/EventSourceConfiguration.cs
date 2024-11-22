using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Extensions;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Repositories.Redis;
using EventStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Redis.OM;
using TDiscover;

namespace EventStorage.Configurations;

public class EventSourceConfiguration(IServiceCollection services, string schema) 
    : EventStorageConfiguration(services, schema)
{
    public EventStore Source { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public List<IProjection> Projections = [];

    // initialize stores while app spins up
    public EventSourceConfiguration Initialize()
    {
        if(Projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
        {
            var p = Projections.First(x => x.Configuration.Store == ProjectionStore.Redis);
            ServiceCollection.AddSingleton(new RedisConnectionProvider(p.Configuration.ConnectionString));
        }
        ServiceCollection.AddSingleton<IRedisService, RedisService>();
        var sourceType = Td.FindByType<IEventSource>()?? typeof(IEventSource);
        var initializerType = typeof(StoreInitializer<>).MakeGenericType(sourceType);
        ServiceCollection.AddSingleton(typeof(IHostedService), sp =>
            Activator.CreateInstance(initializerType, sp, Projections)?? default!);
        return this;
    }
    // pre-compiling projections for future use if more perf needed
    public EventSourceConfiguration ConfigureProjectionRestorer()
    {
        Dictionary<IProjection, List<MethodInfo>> projections = [];
        foreach (var projection in Projections)
        {
            var methods = projection.GetMethods();
            projections.Add(projection, methods.ToList());
        }
        ServiceCollection.AddSingleton<IProjectionRestorer>(sp => new ProjectionRestorer(sp, projections));
        return this;
    }
    public EventSourceConfiguration RunAsyncProjectionEngine()
    {
        ServiceCollection.AddSingleton<IAsyncProjectionPool, AsyncProjectionPool>();
        var sourceType = Td.FindByType<IEventSource>()?? typeof(IEventSource);
        var engineType = typeof(AsyncProjectionEngine<>).MakeGenericType(sourceType);
        ServiceCollection.AddSingleton(typeof(IHostedService), engineType);
        return this;
    }
}