using System.Collections.Concurrent;
using EventStorage.Events;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public interface IAsyncProjectionPoll
{
    Task BlockAsync(CancellationToken token);
    Task PollAsync(CancellationToken token);
    Func<CancellationToken, Task>? DequeueAsync();
    void Release(Func<CancellationToken, Task> project);
    void Release();
    ConcurrentQueue<Func<CancellationToken, Task>> QueuedProjections { get; }
}
public class AsyncProjectionPoll : IAsyncProjectionPoll
{
    private readonly SemaphoreSlim _pool = new(1, 1);
    public ConcurrentQueue<Func<CancellationToken, Task>> QueuedProjections => [];
    public async Task BlockAsync(CancellationToken token) => await _pool.WaitAsync(token);
    public async Task PollAsync(CancellationToken token) => await BlockAsync(token);
    public Func<CancellationToken, Task>? DequeueAsync()
    {
        QueuedProjections.TryDequeue(out var task);
        return task;
    }
    public void Release(Func<CancellationToken, Task> project)
    {
        QueuedProjections.Enqueue(project);
        _pool.Release();
    }
    public void Release() => _pool.Release();
}