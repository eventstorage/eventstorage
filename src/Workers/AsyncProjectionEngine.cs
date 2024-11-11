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

public class AsyncProjectionEngine : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _logger;
    private readonly IAsyncProjectionPoll _poll;
    private readonly IEnumerable<IProjection> _projections;
    private readonly EventStorage<IEventSource> _storage;
    public AsyncProjectionEngine(EventStorage<IEventSource> storage, IServiceProvider scope)
    {
        _sp = scope;
        _logger = _sp.GetRequiredService<ILogger<AsyncProjectionEngine>>();
        _poll = _sp.GetRequiredService<IAsyncProjectionPoll>();
        _projections = _sp.GetServices<IProjection>().Where(x => x.Mode == ProjectionMode.Async);
        _storage = storage;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Polling for async projections.");
                await _poll.PollAsync(stoppingToken);
                _logger.LogInformation("Received projections, running projection engine.");
                // await StartProjection(stoppingToken);
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
        var aggregate = Td.FindByType<IEventSource>()?? default!;
        var storage = _sp.GetRequiredService(typeof(IEventStorage<>).MakeGenericType(aggregate));
        
        // var checkpoint = await storage.LoadCheckpoint();
        // var events = await _eventStorage.LoadEventsPastCheckpoint(checkpoint);
        // if(!events.Any())
        //     return;

        // // var groupById = events.GroupBy(x => x.SourceId);
        // foreach (var eventSource in groupById)
        // {
            
        // }
    }
    // private IEnumerable<SourcedEvent> PullPendingEvents()
    // {
    //     var aggregate = Td.
    //     var eventStorage = typeof(IEventStorage<>).MakeGenericType();
    // }
}