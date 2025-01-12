using System.Collections.Concurrent;
using System.Reflection;
using EventStorage.Events;
using EventStorage.Extensions;
using EventStorage.Infrastructure;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public class AsyncProjectionEngine<T>(IServiceProvider sp,
    Dictionary<Projection, IEnumerable<MethodInfo>> projections) : BackgroundService
{
    private ILogger _logger = sp.GetRequiredService<ILogger<AsyncProjectionEngine<T>>>();
    private IAsyncProjectionPool _pool = sp.GetRequiredService<IAsyncProjectionPool>();
    private IEventStorage<T> _storage = sp.GetRequiredService<IEventStorage<T>>();
    private readonly IServiceScopeFactory _scope = sp.GetRequiredService<IServiceScopeFactory>();
    private readonly ConcurrentDictionary<Projection, Task> _projectionTasks = [];
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
                    restores.Add(Task.Run(() => _storage.RestoreProjections(envelop, scope), ct));
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
    public async Task StartPolling(CancellationToken ct)
    {
        await _pool.PollAsync(ct);

        var x = _pool.QueuedProjections.Count;
        _logger.Log($"{x} queued items are pending exceution from projection pool.");
        while(_pool.Peek() != null && !ct.IsCancellationRequested)
        {
            var queuedItem = _pool.Peek()?? default!;
            var item = queuedItem(ct);
            var events = await _storage.LoadEventSource(item.LId);
            foreach (var projection in projections)
            {
                if(!projection.Value.Subscribes(item.SourcedEvents))
                    continue;
                _projectionTasks.TryAdd(projection.Key, Task.Run(() =>
                    _storage.RestoreProjection(projection.Key, events, sp), ct));
            }
            
            _logger.Log($"Strated restoring {_projectionTasks.Count} projections for source {item.LId}.");
            foreach (var task in _projectionTasks)
            {
                try
                {
                    await task.Value;
                    _projectionTasks.TryRemove(task);
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000);
                    if(_logger.IsEnabled(LogLevel.Error))
                        _logger.LogError($"Failure running projection task {task.Key}.{Environment.NewLine} {e.StackTrace}");
                    throw;
                }
            }
            _logger.Log($"Done restoring {_projectionTasks.Count} projections for source {item.LId}.");
            _pool.Dequeue();
        }
    }
}