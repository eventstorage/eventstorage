using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Extensions;
using EventStorage.Infrastructure;
using EventStorage.Projections;
using EventStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Redis.OM;

namespace EventStorage.Configurations;

public class EventSourceConfiguration(IServiceCollection services, string? schema = null, string? conn = null)
    : EventStorageConfiguration(services, schema, conn)
{
    internal Type T { get; set; } = default!;
    internal Dictionary<Projection, IEnumerable<MethodInfo>> ConfiguredProjections = [];

    // initialize stores while app spins up
    public EventSourceConfiguration Initialize()
    {
        if(Projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
        {
            var p = Projections.First(x => x.Configuration.Store == ProjectionStore.Redis);
            ServiceCollection.AddSingleton(new RedisConnectionProvider(p.Configuration.ConnectionString));
        }
        ServiceCollection.AddSingleton<IRedisService, RedisService>();

        var initializerType = typeof(StoreInitializer<>).MakeGenericType(T);
        ServiceCollection.AddSingleton(typeof(IHostedService), sp =>
            Activator.CreateInstance(initializerType, sp, Projections)?? default!);
        return this;
    }
    // pre-compiling projections for future use if more perf needed
    public EventSourceConfiguration ConfigureProjections()
    {
        foreach (var projection in Projections.Where(x => x.Mode == ProjectionMode.Async))
        {
            var methods = projection.GetMethods();
            ConfiguredProjections.Add(projection, methods.ToList());
        }
        ServiceCollection.AddSingleton<IProjectionRestorer>(sp => new ProjectionRestorer(sp));
        return this;
    }
    public EventSourceConfiguration RunProjectionEngine()
    {
        ServiceCollection.AddSingleton<IAsyncProjectionPool, AsyncProjectionPool>();
        var engineType = typeof(AsyncProjectionEngine<>).MakeGenericType(T);
        ServiceCollection.AddSingleton(typeof(IHostedService), sp =>
            Activator.CreateInstance(engineType, sp, ConfiguredProjections)?? default!);
        return this;
    }
}