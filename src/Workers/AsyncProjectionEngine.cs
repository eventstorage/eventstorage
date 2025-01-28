using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using EventStorage.Events;
using EventStorage.Extensions;

// using EventStorage.Extensions;
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
    private readonly ConcurrentDictionary<Projection, Dictionary<long, Task>> _projectionTasks = [];
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
        try
        {
            foreach (var projection in projections)
            {
                var pname = projection.Key.GetType().Name;
                // fix the checkpoint maxSeq bug
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
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.Error($"Failure restoring projections.{Environment.NewLine} {e.StackTrace}.");
            // throw;
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
        _pool.Poll(ct);
        var x = _pool.QueuedProjections.Count;
        _logger.Log($"{x} queued items pending exceution from projection pool.");
        IEnumerable<EventEnvelop> events = [];
        while(_pool.Peek() != null && !ct.IsCancellationRequested)
        {
            var queuedItem = _pool.Peek()?? default!;
            var item = queuedItem(ct);
            try
            {
                events = await _storage.LoadEventSource(item.LId);
            }
            catch(Exception e)
            {
                _logger.Error($"{Environment.NewLine}{e.StackTrace}.");
                break;
            }
            Parallel.ForEach(projections, p => {
                if(!p.Value.Subscribes(item.SourcedEvents) && item.SourcedEvents.Any())
                    return;
                var task = Task.Run(() => _storage.RestoreProjection(p.Key, sp, events.Envelop()), ct)
                    .ContinueWith((_, o) => {
                        if(_.IsFaulted || ct.IsCancellationRequested)
                            throw _.Exception?.InnerException?? default!;
                        _storage.SaveCheckpoint(new(p.Key.GetType().Name, events.Last().Seq, 0));
                        _projectionTasks[p.Key].Remove(item.LId);
                    }, TaskContinuationOptions.None, ct);

                _projectionTasks.AddOrUpdate(p.Key, (k) => new(){{item.LId, task}}, (k, v) => {
                    v.Add(item.LId, task); return v;
                });
            });
            
            x = _projectionTasks.Count(x => x.Value.Values.Count != 0);
            _logger.Log($"Restoring {x} projections for event source {item.LId}.");
            var tasks = _projectionTasks.SelectMany(x => x.Value.Values.Select(x => x));
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(@$"Failure projecting event source {item.LId}: {e.Message}.
                    {Environment.NewLine}{e.StackTrace}.");
            }
            // foreach (var task in tasks)
            // {
            //     await task;
            // }
            _pool.Dequeue();
            x = _projectionTasks.Count(x => x.Value.Values.Count == 0);
            _logger.Log($"{x} projections are restored for event source {item.LId}.");
        }
    }
}