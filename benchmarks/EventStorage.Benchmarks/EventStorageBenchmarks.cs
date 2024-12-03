using BenchmarkDotNet.Attributes;
using EventStorage.Benchmarks.Commands;
using EventStorage.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Benchmarks;

public class EventStorageBenchmarks
{
    private string _sampleSourceId { get; set; } = "8766";
    private static readonly IServiceProvider _sp = new ServiceCollection().ConfigureContainer();
    private IEventStorage<OrderBooking> _storage = default!;
    [GlobalSetup]
    public void Setup()
    {
        _storage = _sp.GetRequiredService<IEventStorage<OrderBooking>>();
        _storage.InitSource().GetAwaiter().GetResult();
    }
    [Benchmark]
    public async Task GetOrder_Transient()
    {
        var order = await _storage.Project<Order>(_sampleSourceId);
    }
    [Benchmark]
    public async Task GetOrderDetail_AsyncPostgres()
    {
        var order = await _storage.Project<OrderDetail>(_sampleSourceId);
    }
    [Benchmark]
    public async Task GetOrderDocument_AsyncRedis()
    {
        var order = await _storage.Project<OrderDocument>(_sampleSourceId);
    }
    [GlobalCleanup]
    public void Cleanup()
    {
           
    }
}