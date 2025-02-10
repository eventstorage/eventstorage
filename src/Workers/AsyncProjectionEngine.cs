using System.Collections.Concurrent;
using System.Collections.Immutable;
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
    private readonly ConcurrentDictionary<Projection, Dictionary<long, Func<Task>>> _projectionTasks = [];
    private readonly SemaphoreSlim _dlqPool = new(initialCount: 1, maxCount:1);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Log($"Started restoring {projections.Count} async projections.");
        await RestoreProjections(stoppingToken);
        _logger.Log($"{projections.Count} projections are synced with event storage.");
        _ = Task.Run(() => StartDlq(stoppingToken), stoppingToken);
        while(!stoppingToken.IsCancellationRequested)
        {
            _logger.Log("Polling for tasks from projection pool.");
            await StartPolling(stoppingToken);
        }
    }
    private async Task RestoreProjections(CancellationToken ct)
    {
        try
        {
            var maxSeq = await _storage.LoadMaxSequence();
            await projections.ParallelForEach(1, async (projection, ct) => {
                Checkpoint checkpoint = await _storage.LoadCheckpoint(projection.Key);
                checkpoint = checkpoint with { MaxSeq = maxSeq };
                var pname = projection.Key.GetType().Name;
                _logger.Log($"Started restoring {pname} from checkpoint {checkpoint.Seq}.");
                while(true)
                {
                    ct.ThrowIfCancellationRequested();
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
                    _logger.Log($"Restored {pname} and saved checkpoint {checkpoint.Seq}.");
                }
                _logger.Log($"{pname} is synced with event storage, checkpoint {checkpoint.Seq}.");
            }, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.Error($"{e.Message}.{Environment.NewLine}{e.StackTrace}.");
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
    private async Task StartPolling(CancellationToken ct)
    {
        await _pool.PollAsync(ct);
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
                _logger.Error($"Failure loading event source {item.LId}. {e.Message}.");
                break;
            }
            x = 0;
            _logger.Log($"Restoring {projections.Count} projections for event source {item.LId}.");
            await Parallel.ForEachAsync(projections, async (p, ct) => {
                var pname = p.Key.GetType().Name;
                // subscribes or reprojection wanted
                if(!p.Value.Subscribes(item.SourcedEvents) && item.SourcedEvents.Any())
                    return;
                var task = () => _storage.RestoreProjection(p.Key, sp, events.Envelop())
                .ContinueWith((_, o) =>
                {
                    if(_.IsFaulted)
                    {
                        _logger.Error(@$"Error restoring {pname} for event source {item.LId}.");
                        throw _.Exception?? default!;
                    }
                    if(item.SourcedEvents.Any())
                        _storage.SaveCheckpoint(new(pname, events.Last().Seq, 0));
                    _projectionTasks[p.Key].Remove(item.LId);
                    Interlocked.Increment(ref x);
                    _logger.Log($"Restored {pname} for event source {item.LId}.");
                }, TaskContinuationOptions.None, ct);
                
                _projectionTasks.AddOrUpdate(p.Key, (k) => new(){{item.LId, task}}, (k, v) =>{
                    v[item.LId] = task; return v;});
                await Task.CompletedTask;
            });

            await _projectionTasks.ParallelForEach(projections.Count, async (_, ct) =>
            {
                var tasks = _.Value.OrderBy(x => x.Key).Select(x => x.Value);
                foreach (var task in tasks)
                {
                    try{await task();}
                    catch
                    {
                        if(_dlqPool.CurrentCount == 0)
                            _dlqPool.Release();
                        return;
                    }
                }
            }, ct);
            _logger.Log($"{x} projections are restored for event source {item.LId}.");
            _pool.Dequeue();
        }
    }
    private async Task StartDlq(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            await _dlqPool.WaitAsync(ct);
            await Task.Delay(5000, ct);
            var tasks = _projectionTasks.Values.SelectMany(d => d.Values);
            while(tasks.Any() && !ct.IsCancellationRequested)
            {
                await _projectionTasks.ParallelForEach(projections.Count, async (_, ct) =>
                {
                    var tasks = _.Value.OrderBy(x => x.Key).Select(x => x.Value);
                    foreach (var task in tasks)
                    {
                        try{await task();}
                        catch (Exception e)
                        {
                            _logger.Error($"{e.Message}.");
                            await Task.Delay(5000, ct);
                            return;
                        }
                    }
                }, ct);
            }
        }
    }
}