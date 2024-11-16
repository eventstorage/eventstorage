using System.Collections.Concurrent;
using EventStorage.Events;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public interface IAsyncProjectionPoll
{
    Task PollAsync(CancellationToken token);
    void Release();
    ConcurrentQueue<IEnumerable<SourcedEvent>> ProjectionsQueue {  get; }
}
public class AsyncProjectionPoll : IAsyncProjectionPoll
{
    private readonly SemaphoreSlim _pool = new(1, 1);
    public ConcurrentQueue<IEnumerable<SourcedEvent>> ProjectionsQueue => [];
    public async Task PollAsync(CancellationToken token) => await _pool.WaitAsync(token);
    public void Release() => _pool.Release();
}