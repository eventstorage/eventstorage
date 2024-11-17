using System.Collections.Concurrent;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public interface IAsyncProjectionPoll
{
    Task BlockAsync(CancellationToken token);
    Task PollAsync(CancellationToken token);
    Func<IServiceScopeFactory, CancellationToken, Task>? DequeueAsync();
    void Release(Func<IServiceScopeFactory, CancellationToken, Task> project);
    void Release();
    ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task>> QueuedProjections { get; }
}
public class AsyncProjectionPoll : IAsyncProjectionPoll
{
    private readonly SemaphoreSlim _pool = new(1, 1);
    private readonly ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task>> _queue = [];
    public ConcurrentQueue<Func<IServiceScopeFactory, CancellationToken, Task>> QueuedProjections => _queue;
    public async Task BlockAsync(CancellationToken token) => await _pool.WaitAsync(token);
    public async Task PollAsync(CancellationToken token) => await BlockAsync(token);
    public Func<IServiceScopeFactory, CancellationToken, Task>? DequeueAsync()
    {
        QueuedProjections.TryDequeue(out var task);
        return task;
    }
    public void Release(Func<IServiceScopeFactory, CancellationToken, Task> project)
    {
        _queue.Enqueue(project);
        _pool.Release();
    }
    public void Release() => _pool.Release();
}