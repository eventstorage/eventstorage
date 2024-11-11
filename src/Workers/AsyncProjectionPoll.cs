using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public interface IAsyncProjectionPoll
{
    Task PollAsync(CancellationToken token);
    void Release();
}
public class AsyncProjectionPoll : IAsyncProjectionPoll
{
    private readonly SemaphoreSlim _pool = new(1, 1);
    public async Task PollAsync(CancellationToken token) => await _pool.WaitAsync(token);
    public void Release() => _pool.Release();
}