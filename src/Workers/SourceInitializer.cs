using EventStorage.AggregateRoot;
using EventStorage.Repositories;
using Microsoft.Extensions.Hosting;

namespace EventStorage.Workers;

internal class SourceInitializer(IEventStorage<IEventSource> eventSource) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // await Task.Run( () =>
        // {
        //     eventSource.InitSource();
        // }, cancellationToken);

        // block thread on source initialization
        await eventSource.InitSource();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
