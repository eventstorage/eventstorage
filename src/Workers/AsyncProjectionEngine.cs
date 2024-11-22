using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public class AsyncProjectionEngine<T> : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private IServiceProvider _sp => _scope.CreateScope().ServiceProvider;
    private ILogger _logger => _sp.GetRequiredService<ILogger<AsyncProjectionEngine<T>>>();
    private IAsyncProjectionPool _poll => _sp.GetRequiredService<IAsyncProjectionPool>();
    private IEventStorage<T> _storage => _sp.GetRequiredService<IEventStorage<T>>();
    public AsyncProjectionEngine(IServiceScopeFactory scope)
    {
        _scope = scope;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started projection engine, restoring projections...");
        await RestoreProjections(stoppingToken);
        _logger.LogInformation("Done restoring and synchronizing projections.");

        while(!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Polling for async projections to get released.");
            await StartPolling(stoppingToken);
        }
    }
    public async Task RestoreProjections(CancellationToken ct)
    {
        try
        {
            var checkpoint = await _storage.LoadCheckpoint();
            _logger.LogInformation($"Starting restoration from checkpoint {checkpoint.Seq}.");
            while(!ct.IsCancellationRequested)
            {
                var events = await _storage.LoadEventsPastCheckpoint(checkpoint);
                if(!events.Any())
                    break;
                _logger.LogInformation($"Loaded batch of {events.Count()} events to restore projections.");

                var groupedBySource = (from e in events
                                        group e by e.LId into groupedById
                                        select groupedById).Select(x => new EventSourceEnvelop(
                                            x.First().LId, x.First().GId, x.Select(x => x.SourcedEvent))
                                        );

                IList<Task> restores = [];
                foreach (var source in groupedBySource)
                {
                    EventSourceEnvelop envelop = await CheckEventSourceIntegrity(source);
                    restores.Add(Task.Run(() => _storage.RestoreProjections(envelop, _scope), ct));
                }
                Task.WaitAll(restores.ToArray(), ct);

                checkpoint = checkpoint with { Seq = events.Last().Seq };
                await _storage.SaveCheckpoint(checkpoint);
                _logger.LogInformation($"Restored projections for batch of {events.Count()} events.");
            }
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failure restoring projections.{Environment.NewLine} {e.StackTrace}.");
            throw;
        }
    }
    private async Task<EventSourceEnvelop> CheckEventSourceIntegrity(EventSourceEnvelop source)
    {
        IEnumerable<SourcedEvent> ordered = source.SourcedEvents.OrderBy(x => x.Version);
        if(ordered.First().Version != 1)
        {
            var eventSource = await _storage.LoadEventSource(source.LId);
            source = source with { SourcedEvents = eventSource.Select(x => x.SourcedEvent) };
        }
        return source;
    }
    public async Task StartPolling(CancellationToken stoppingToken)
    {
        await _poll.PollAsync(stoppingToken);

        var x = _poll.QueuedProjections.Count;
        _logger.LogInformation($"Executing {x} pending projection task(s).");
        while(!stoppingToken.IsCancellationRequested)
        {
            var projectTask = _poll.Peek();
            if(projectTask == null)
                break;
            try
            {
                long sourceId = await projectTask(_scope, stoppingToken);
                var source = await _storage.LoadEventSource(sourceId);
                Checkpoint c = new(0, source.Last().Seq, CheckpointType.Projection, typeof(T).Name);
                await _storage.SaveCheckpoint(c);
                _poll.Dequeue();
                _logger.LogInformation($"Done executing projection task for source id {sourceId}.");
            }
            catch
            {
                break;
            }
        }
    }
}