using EventStorage.AggregateRoot;
using EventStorage.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventStorage.Workers;

public class AsyncProjectionEngine(IServiceProvider sp) : BackgroundService
{
    private readonly ILogger _logger = sp.GetRequiredService<ILogger<AsyncProjectionEngine>>();
    private readonly IAsyncProjectionPoll _poll = sp.GetRequiredService<IAsyncProjectionPoll>();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting async projection engine.");
        _logger.LogInformation($"Synchronizing projections has started.");
        _logger.LogInformation($"Synchronizing projections has ended");
        await StartPolling(stoppingToken);
    }
    public async Task StartPolling(CancellationToken stoppingToken)
    {
        try
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Polling for async projections.");
                await _poll.Poll(stoppingToken);
                _logger.LogInformation("Received async projections.");
                await Task.CompletedTask;
            }
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Async projection failure. {e.Message}");
            throw;
        }
    }
}