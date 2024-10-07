using AsyncHandler.EventSourcing.Repositories;
using Microsoft.Extensions.Hosting;

namespace AsyncHandler.EventSourcing.Workers;

internal class SourceInitializer(IEventSource<IAggregateRoot> eventSource) : IHostedService
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
