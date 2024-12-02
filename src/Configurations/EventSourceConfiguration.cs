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
    public EventSourceConfiguration ConfigureProjectionRestorer()
    {
        Dictionary<Projection, List<MethodInfo>> projections = [];
        foreach (var projection in Projections)
        {
            var methods = projection.GetMethods();
            projections.Add(projection, methods.ToList());
        }
        ServiceCollection.AddSingleton<IProjectionRestorer>(sp => new ProjectionRestorer(sp));
        // ServiceCollection.AddSingleton<IProjectionRestorer<T>>(sp => new ProjectionRestorer<T(sp, projections));
        return this;
    }
    public EventSourceConfiguration RunAsyncProjectionEngine()
    {
        ServiceCollection.AddSingleton<IAsyncProjectionPool, AsyncProjectionPool>();
        var engineType = typeof(AsyncProjectionEngine<>).MakeGenericType(T);
        ServiceCollection.AddSingleton(typeof(IHostedService), engineType);
        return this;
    }
}