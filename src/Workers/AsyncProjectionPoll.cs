using System.Collections.Concurrent;

namespace EventStorage.Workers;

public interface IAsyncProjectionPoll
{
    Task Poll(CancellationToken token);
    void Release();
}
public class AsyncProjectionPoll : IAsyncProjectionPoll
{
    private readonly SemaphoreSlim _semaphore = new(1);
    // private readonly ConcurrentQueue<Task> _projections = new();
    public async Task Poll(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);
    }
    public void Release()
    {
        // _projections.Enqueue(task);
        _semaphore.Release();
    }
}