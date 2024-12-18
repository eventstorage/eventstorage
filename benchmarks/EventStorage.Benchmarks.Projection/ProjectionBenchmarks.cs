using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EventStorage.Benchmarks.Events;
using EventStorage.Events;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Benchmarks.Projections;

// [MemoryDiagnoser]
// [SimpleJob(RuntimeMoniker.Net60)]
// [SimpleJob(RuntimeMoniker.Net80)]
public class ProjectionBenchmarks
{
    private static readonly IServiceProvider _sp = Container.Build();
    private readonly IProjectionRestorer _projection = _sp.GetRequiredService<IProjectionRestorer>();
    private readonly List<SourcedEvent> _events = [];
    [GlobalSetup]
    public void Setup()
    {
        var orderPlaced = new OrderPlaced();
        var orderConfirmed = new OrderConfirmed();
        _events.Add(orderPlaced with { SourceId = 1, Version = 1 });
        _events.Add(orderConfirmed with { SourceId = 1, Version = 2 });
    }
    [Benchmark]
    public void Project() => _projection.Project<Order>(_events);
}