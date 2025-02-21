using System.Collections.Concurrent;

namespace EventStorage.Extensions;

public static class IEnumerableExtensions
{
    public static Task ParallelAsync<T>(
        this IEnumerable<T> source,
        int dop,
        Func<T, CancellationToken, ValueTask> body) =>
        Parallel.ForEachAsync(source, new ParallelOptions { MaxDegreeOfParallelism = dop }, body);
    public static async Task ParallelForEach<T>(
        this IEnumerable<T> items,
        int dop,
        Func<T, CancellationToken, Task> body,
        CancellationToken ct)
    {
        ConcurrentBag<Exception> exceptions = [];
        using SemaphoreSlim throttler = new(1, dop == 0 ? 1 : dop);
        await Task.WhenAll(
            from item in items
            select Task.Run(async delegate
            {
                await throttler.WaitAsync(ct);
                await body(item, ct).ContinueWith(_ =>
                {
                    ct.ThrowIfCancellationRequested();
                    if(_.IsFaulted)
                        exceptions.Add(_.Exception.InnerException?? _.Exception);
                    throttler.Release();
                }, TaskContinuationOptions.None);
            }, ct)
        );
        ct.ThrowIfCancellationRequested();
        if(!exceptions.IsEmpty)
            throw new AggregateException(exceptions);
    }
}