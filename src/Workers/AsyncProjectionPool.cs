using System.Collections.Concurrent;
using EventStorage.Events;

namespace EventStorage.Workers;

public interface IAsyncProjectionPool
{
    void Poll(CancellationToken token);
    Func<CancellationToken, EventSourceEnvelop>? Dequeue();
    Func<CancellationToken, EventSourceEnvelop>? Peek();
    void Release(Func<CancellationToken, EventSourceEnvelop> envelop);
    ConcurrentQueue<Func<CancellationToken, EventSourceEnvelop>> QueuedProjections { get; }
}
public class AsyncProjectionPool : IAsyncProjectionPool
{
    private readonly SemaphoreSlim _pool = new(1);
    private readonly ConcurrentQueue<Func<CancellationToken, EventSourceEnvelop>> _queue = [];
    public ConcurrentQueue<Func<CancellationToken, EventSourceEnvelop>> QueuedProjections => _queue;
    public void Poll(CancellationToken token) => _pool.Wait(token);
    public Func<CancellationToken, EventSourceEnvelop>? Dequeue()
    {
        QueuedProjections.TryDequeue(out var item);
        return item;
    }
    public Func<CancellationToken, EventSourceEnvelop>? Peek()
    {
        QueuedProjections.TryPeek(out var item);
        return item;
    }
    public void Release(Func<CancellationToken, EventSourceEnvelop> envelop)
    {
        _queue.Enqueue(envelop);
        if(_pool.CurrentCount == 0)
            _pool.Release();
    }
}