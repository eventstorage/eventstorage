using System.Collections.Concurrent;
using EventStorage.Events;

namespace EventStorage.Workers;

public interface IAsyncProjectionPool
{
    Task PollAsync(CancellationToken token);
    Func<CancellationToken, EventSourceEnvelop>? Dequeue();
    Func<CancellationToken, EventSourceEnvelop>? Peek();
    void Release(Func<CancellationToken, EventSourceEnvelop> envelop);
    ConcurrentQueue<Func<CancellationToken, EventSourceEnvelop>> QueuedProjections { get; }
}
public class AsyncProjectionPool : IAsyncProjectionPool
{
    private readonly SemaphoreSlim _pool = new(1, 1);
    private readonly ConcurrentQueue<Func<CancellationToken, EventSourceEnvelop>> _queue = [];
    public ConcurrentQueue<Func<CancellationToken, EventSourceEnvelop>> QueuedProjections => _queue;
    public async Task PollAsync(CancellationToken token) => await _pool.WaitAsync(token);
    public Func<CancellationToken, EventSourceEnvelop>? Dequeue()
    {
        QueuedProjections.TryDequeue(out var task);
        return task;
    }
    public Func<CancellationToken, EventSourceEnvelop>? Peek()
    {
        QueuedProjections.TryPeek(out var task);
        return task;
    }
    public void Release(Func<CancellationToken, EventSourceEnvelop> projection)
    {
        _queue.Enqueue(projection);
        if(_pool.CurrentCount == 0)
            _pool.Release();
    }
}