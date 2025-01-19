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

internal sealed class AsyncProjectionEngine<T>(IServiceProvider sp,
    Dictionary<Projection, IEnumerable<MethodInfo>> projections) : BackgroundService
{
    private readonly ILogger _logger = TLogger.Create<AsyncProjectionEngine<T>>();
    private readonly IAsyncProjectionPool _pool = sp.GetRequiredService<IAsyncProjectionPool>();
    private readonly IEventStorage<T> _storage = sp.CreateScope().ServiceProvider.GetRequiredService<IEventStorage<T>>();
    private readonly IServiceScopeFactory _scope = sp.GetRequiredService<IServiceScopeFactory>();
    private readonly ConcurrentDictionary<Projection, Func<Task>> _projectionTasks = [];
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Log($"Started restoring {projections.Count} async projections...");
        await RestoreProjections(stoppingToken);
        _logger.Log($"Done restoring {projections.Count} async projections.");
        while(!stoppingToken.IsCancellationRequested)
        {
            _logger.Log("Polling for async projections to get released.");
            await StartPolling(stoppingToken);
        }
    }
    public async Task RestoreProjections(CancellationToken ct)
    {
        foreach (var projection in projections)
        {
            try
            {
                var pname = projection.Key.GetType().Name;
                var checkpoint = await _storage.LoadCheckpoint(projection.Key);
                _logger.Log($"Started restoring {pname} from checkpoint {checkpoint.Seq}.");
                while(!ct.IsCancellationRequested)
                {
                    var events = await _storage.LoadEventsPastCheckpoint(checkpoint);
                    if(!events.Any())
                        break;
                    _logger.Log($"Loaded batch of {events.Count()} events to restore {pname}.");

                    var groupedByEventSource = (from e in events
                                                group e by e.LId into groupedById
                                                select groupedById)
                                                .Select(x => new EventSourceEnvelop(x.First().LId,
                                                x.First().GId, x.Select(x => x.SourcedEvent)));
                    
                    IList<EventSourceEnvelop> sources = [];
                    foreach (var eventSource in groupedByEventSource)
                    {
                        var source = await CheckEventSourceIntegrity(eventSource);
                        if(!projection.Value.Subscribes(source.SourcedEvents))
                            continue;
                        sources.Add(source);
                    }
                    await _storage.RestoreProjection(projection.Key, sp, [.. sources]);
                    checkpoint = checkpoint with { Seq = events.Last().Seq };
                    await _storage.SaveCheckpoint(checkpoint);
                }
                _logger.Log($"Done restoring {pname} with checkpoint {checkpoint.Seq}.");
            }
            catch (Exception e)
            {
                if(_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError($"Failure restoring {nameof(projection)}.{Environment.NewLine} {e.StackTrace}.");
            }
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
        _logger.Log($"{x} queued items pending exceution from projection pool.");

        while(_pool.Peek() != null && !ct.IsCancellationRequested)
        {
            var queuedItem = _pool.Peek()?? default!;
            var item = queuedItem(ct);
            var events = await _storage.LoadEventSource(item.LId);
            foreach (var projection in projections)
            {
                if(!projection.Value.Subscribes(item.SourcedEvents) && item.SourcedEvents.Any())
                    continue;
                var task = () => _storage.RestoreProjection(projection.Key, sp, events.Envelop())
                    .ContinueWith(x =>
                    {
                        _storage.SaveCheckpoint(new(projection.Key.GetType().Name, events.Last().Seq, 0));
                        _projectionTasks.Remove(projection.Key, out var value);
                    });
                _projectionTasks.TryAdd(projection.Key, task);
            }
            x = _projectionTasks.Count;
            _logger.Log($"Started restoring {x} projections for event source {item.LId}.");
            try
            {
                await Task.WhenAll(_projectionTasks.Values.Select(task => task()));
            }
            catch (Exception e)
            {
                _logger.Error($"Projection task failed.{Environment.NewLine}{e.StackTrace}");
                break;
            }
            finally
            {
                _pool.Dequeue();
                _logger.Log($"Done restoring {x} projections for event source {item.LId}.");
            }
        }
    }
}