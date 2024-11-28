using BenchmarkDotNet.Attributes;
using EventStorage.Benchmarks.Commands;
using EventStorage.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Benchmarks;

public class EventStorageBenchmarks
{
    private static readonly IServiceProvider _sp = Container.Build();
    private long LongId { get; set; } = 0;
    private Guid GuidId { get; set; } = Guid.NewGuid();
    private IEventStorage<OrderBookingLong> StorageLong { get; set; } = default!;
    private IEventStorage<OrderBookingGuid> StorageGuid { get; set; } = default!;
    [GlobalSetup]
    public async Task Setup()
    {
        StorageLong = _sp.GetRequiredService<IEventStorage<OrderBookingLong>>();
        await StorageLong.InitSource();
        StorageGuid = _sp.GetRequiredService<IEventStorage<OrderBookingGuid>>();
        await StorageGuid.InitSource();
    }
    [Benchmark]
    public async Task PlaceAndConfirmOrder_Long()
    {
        var aggregate = await StorageLong.CreateOrRestore();
        aggregate.PlaceOrder(new PlaceOrder("", 0, ""));
        aggregate.ConfirmOrder(new ConfirmOrder());
        await StorageLong.Commit(aggregate);
        LongId = aggregate.SourceId;
    }
    [Benchmark]
    public async Task PlaceAndConfirmOrder_Guid()
    {
        var aggregate = await StorageGuid.CreateOrRestore();
        aggregate.PlaceOrder(new PlaceOrder("", 0, ""));
        aggregate.ConfirmOrder(new ConfirmOrder());
        await StorageGuid.Commit(aggregate);
        GuidId = aggregate.SourceId;
    }
    [Benchmark]
    public async Task GetOrderByLong()
    {
        var order = await StorageLong.Project<Order>(LongId.ToString());
    }
    [Benchmark]
    public async Task GetOrderByGuid()
    {
        var order = await StorageGuid.Project<Order>(GuidId.ToString());
    }
}