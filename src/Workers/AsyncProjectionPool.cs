using System.Collections.Concurrent;
using EventStorage.AggregateRoot;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Workers;

public interface IAsyncProjectionPool
{
    Task PollAsync(CancellationToken token);
    Func<IServiceScopeFactory, CancellationToken, Task<long>>? Dequeue();
    Func<IServiceScopeFactory, CancellationToken, Task<long>>? Peek();
    void Release(Func<IServiceScopeFactory, CancellationToken, Task<long>> project);
    ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task<long>>> QueuedProjections { get; }
}
public class AsyncProjectionPool : IAsyncProjectionPool
{
    private readonly SemaphoreSlim _pool = new(1, 1);
    private readonly ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task<long>>> _queue = [];
    public ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task<long>>> QueuedProjections => _queue;
    public async Task PollAsync(CancellationToken token) => await _pool.WaitAsync(token);
    public Func<IServiceScopeFactory, CancellationToken, Task<long>>? Dequeue()
    {
        QueuedProjections.TryDequeue(out var task);
        return task;
    }
    public Func<IServiceScopeFactory, CancellationToken, Task<long>>? Peek()
    {
        QueuedProjections.TryPeek(out var task);
        return task;
    }
    public void Release(Func<IServiceScopeFactory, CancellationToken, Task<long>> project)
    {
        _queue.Enqueue(project);
        if(_pool.CurrentCount == 0)
            _pool.Release();
    }
}