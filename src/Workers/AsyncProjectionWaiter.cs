using System.Collections.Concurrent;

namespace EventStorage.Workers;

public interface IAsyncProjectionWaiter
{
    Task Wait(CancellationToken token);
    Task Release(Task task);
}
public class AsyncProjectionWaiter : IAsyncProjectionWaiter
{
    private readonly SemaphoreSlim _semaphore = new(0);
    // private readonly ConcurrentQueue<Task> _projections = new();
    public async Task Wait(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
    }
    public async Task Release(Task task)
    {
        // _projections.Enqueue(task);
        _semaphore.Release();
        await Task.CompletedTask;
    }
}