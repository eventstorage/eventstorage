using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Repositories.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TDiscover;

namespace EventStorage.Workers;

public class AsyncProjectionEngine(IEventStorage<IEventSource> storage, IServiceProvider sp) : BackgroundService
{
    private readonly ILogger _logger = sp.GetRequiredService<ILogger<AsyncProjectionEngine>>();
    private readonly IAsyncProjectionPoll _poll = sp.GetRequiredService<IAsyncProjectionPoll>();
    private readonly IEnumerable<IProjection> _projections = sp.GetServices<IProjection>();
    private readonly IRedisService _redis = sp.GetRequiredService<IRedisService>();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started projection engine, restoring projections...");
        await RestoreProjections(stoppingToken);
        _logger.LogInformation("Restored and synced all async projections.");

        while(!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Polling for new async projections.");
            await StartPolling(stoppingToken);
        }
    }
    public async Task RestoreProjections(CancellationToken stoppingToken)
    {
        try
        {
            await _poll.BlockAsync(stoppingToken);
            var checkpoint = await storage.LoadCheckpoint();
            while(!stoppingToken.IsCancellationRequested)
            {
                var events = await storage.LoadEventsPastCheckpoint(checkpoint);
                _logger.LogInformation($"Loaded batch of {events.Count()} events to restore projections.");
                if(!events.Any())
                    break;
                var groupedBySource = (from e in events 
                                    group e by e.LongSourceId into groupedById
                                    select groupedById).Select(x => (x.First().LongSourceId,
                                    x.First().GuidSourceId, x.Select(x => x.SourcedEvent)));

                IList<Task> restores = [];
                foreach (var source in groupedBySource)
                {
                    EventSourceEnvelop envelop = new(source.LongSourceId, source.GuidSourceId, source.Item3);
                    restores.Add(Task.Run(() => storage.RestoreProjections(envelop), stoppingToken));
                }
                Task.WaitAll(restores.ToArray(), stoppingToken);

                checkpoint = checkpoint with { Sequence = checkpoint.Sequence + events.Count() };
                await storage.SaveCheckpoint(checkpoint);
                _logger.LogInformation($"Restored projections for batch of {events.Count()} events.");
            }
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failure restoring projections. {e.Message}.");
                throw;
        }
        // finally
        // {
        //     _poll.Release();
        // }
    }
    public async Task StartPolling(CancellationToken stoppingToken)
    {
        await _poll.PollAsync(stoppingToken);
        try
        {
            var x = _poll.QueuedProjections.Count;
            _logger.LogInformation($"Restoring {x} pending projections.");
            while(!stoppingToken.IsCancellationRequested)
            {
                var projectTask = _poll.DequeueAsync();
                if(projectTask == null)
                    break;
                await projectTask(stoppingToken);
            }
            _logger.LogInformation($"{x} pending projections were restored.");
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"failure polling for projections. {e.Message}.");
            throw;
        }
    }
}