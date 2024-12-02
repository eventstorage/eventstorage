using BenchmarkDotNet.Attributes;
using EventStorage.Benchmarks.Commands;
using EventStorage.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Benchmarks;

public class EventStorageBenchmarks
{
    private static readonly IServiceProvider _sp = Container.Build();
    private long LongId { get; set; } = 1;
    private IEventStorage<OrderBooking> Storage { get; set; } = default!;
    [GlobalSetup]
    public async Task Setup()
    {
        Storage = _sp.GetRequiredService<IEventStorage<OrderBooking>>();
        await Storage.InitSource();
    }
    // [Benchmark]
    // public async Task PlaceAndConfirmOrder()
    // {
    //     var aggregate = await Storage.CreateOrRestore();
    //     aggregate.PlaceOrder(new PlaceOrder("", 0, ""));
    //     aggregate.ConfirmOrder(new ConfirmOrder());
    //     await Storage.Commit(aggregate);
    //     LongId = aggregate.SourceId;
    // }
    [Benchmark]
    public async Task GetOrder_Transient()
    {
        var order = await Storage.Project<Order>(LongId.ToString());
    }
    [Benchmark]
    public async Task GetOrderDetail_AsyncPostgres()
    {
        var order = await Storage.Project<OrderDetail>(LongId.ToString());
    }
    [Benchmark]
    public async Task GetOrderDocument_AsyncRedis()
    {
        var order = await Storage.Project<OrderDocument>(LongId.ToString());
    }
}