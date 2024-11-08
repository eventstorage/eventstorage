using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public class AsyncProjectionEngine(
    IAsyncProjectionWaiter waiter, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IServiceProvider sp = scopeFactory.CreateScope().ServiceProvider;
    private ILogger _logger => sp.GetRequiredService<ILogger<AsyncProjectionEngine>>();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Waiting for async projections.");
            await waiter.Wait(stoppingToken);
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Async projection failure. {e.Message}");
            throw;
        }
    }
}