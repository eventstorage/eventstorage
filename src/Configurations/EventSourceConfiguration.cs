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

public class EventSourceConfiguration<T>(IServiceCollection services, string? schema = null, string? conn = null)
    : EventStorageConfiguration(services, schema, conn) where T:IEventSource 
{
    internal override List<Projection> Projections { get; set; } = [];

    // initialize stores while app spins up
    public EventSourceConfiguration<T> Initialize()
    {
        if(Projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
        {
            var p = Projections.First(x => x.Configuration.Store == ProjectionStore.Redis);
            ServiceCollection.AddSingleton(new RedisConnectionProvider(p.Configuration.ConnectionString));
        }
        ServiceCollection.AddSingleton<IRedisService<T>, RedisService<T>>();

        ServiceCollection.AddSingleton(typeof(IHostedService), sp => new StoreInitializer<T>(sp, Projections));
        return this;
    }
    // pre-compiling projections for future use if more perf needed
    public EventSourceConfiguration<T> ConfigureProjectionRestorer()
    {
        Dictionary<Projection, List<MethodInfo>> projections = [];
        foreach (var projection in Projections)
        {
            var methods = projection.GetMethods();
            projections.Add(projection, methods.ToList());
        }
        ServiceCollection.AddSingleton<IProjectionRestorer<T>>(sp => new ProjectionRestorer<T>(sp));
        // ServiceCollection.AddSingleton<IProjectionRestorer<T>>(sp => new ProjectionRestorer<T(sp, projections));
        return this;
    }
    public EventSourceConfiguration<T> RunAsyncProjectionEngine()
    {
        ServiceCollection.AddSingleton<IAsyncProjectionPool<T>, AsyncProjectionPool<T>>();
        ServiceCollection.AddHostedService<AsyncProjectionEngine<T>>();
        return this;
    }
}