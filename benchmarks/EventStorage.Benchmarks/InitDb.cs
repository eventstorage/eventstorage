using EventStorage.Benchmarks.Commands;
using EventStorage.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace EventStorage.Benchmarks;

public class InitDb(IEventStorage<OrderBooking> storage) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var numberOfStreams = 50;
        for(int i = 0; i < numberOfStreams; i++)
        {
            var aggregate = await storage.CreateOrRestore();
            aggregate.PlaceOrder(new PlaceOrder("", 0, ""));
            aggregate.ConfirmOrder(new ConfirmOrder());
            await storage.Commit(aggregate);
        }
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}