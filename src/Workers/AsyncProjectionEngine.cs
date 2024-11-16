using System.Security.Cryptography.X509Certificates;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TDiscover;

namespace EventStorage.Workers;

public class AsyncProjectionEngine(
    EventStorage<IEventSource> storage, IServiceProvider sp) : BackgroundService
{
    private readonly ILogger _logger = sp.GetRequiredService<ILogger<AsyncProjectionEngine>>();
    private readonly IAsyncProjectionPoll _poll = sp.GetRequiredService<IAsyncProjectionPoll>();
    private readonly IEnumerable<IProjection> _projections = sp.GetServices<IProjection>();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Polling for async projections.");
                await _poll.PollAsync(stoppingToken);
                _logger.LogInformation("Received projections, running projection engine.");
                await StartProjection(stoppingToken);
            }
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Async projection failure. {e.Message}");
            throw;
        }
    }
    public async Task StartProjection(CancellationToken stoppingToken)
    {
        var checkpoint = await storage.LoadCheckpoint();
        var events = await storage.LoadEventsPastCheckpoint(checkpoint);
        if(!events.Any())
            return;

        var groupById = events.ToDictionary(x => x.SourceId);
        foreach (var eventSource in groupById)
        {
            
        }
    }
    // private IEnumerable<SourcedEvent> PullPendingEvents()
    // {
    //     var aggregate = Td.
    //     var eventStorage = typeof(IEventStorage<>).MakeGenericType();
    // }
}