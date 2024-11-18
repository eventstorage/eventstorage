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

public class AsyncProjectionEngine<T> : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private IServiceProvider _sp => _scope.CreateScope().ServiceProvider;
    private ILogger _logger => _sp.GetRequiredService<ILogger<AsyncProjectionEngine<T>>>();
    private IAsyncProjectionPoll _poll => _sp.GetRequiredService<IAsyncProjectionPoll>();
    private IEventStorage<T> _storage => _sp.GetRequiredService<IEventStorage<T>>();
    public AsyncProjectionEngine(IServiceScopeFactory scope)
    {
        _scope = scope;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started projection engine, restoring projections...");
        await RestoreProjections(stoppingToken);
        _logger.LogInformation("Restored and synchronized all async projections.");

        while(!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Polling for async projections to get released.");
            await StartPolling(stoppingToken);
        }
    }
    public async Task RestoreProjections(CancellationToken stoppingToken)
    {
        try
        {
            var checkpoint = await _storage.LoadCheckpoint();
            var sequence = checkpoint.Sequence;
            _logger.LogInformation($"Starting restoration from checkpoint {checkpoint.Sequence}.");
            while(!stoppingToken.IsCancellationRequested)
            {
                var events = await _storage.LoadEventsPastCheckpoint(checkpoint);
                if(!events.Any())
                    break;
                _logger.LogInformation($"Loaded batch of {events.Count()} events to restore projections.");

                var groupedBySource = (from e in events 
                                    group e by e.LongSourceId into groupedById
                                    select groupedById).Select(x => (x.First().LongSourceId,
                                    x.First().GuidSourceId, x.Select(x => x.SourcedEvent)));
                IList<Task> restores = [];
                foreach (var source in groupedBySource)
                {
                    EventSourceEnvelop envelop = new(source.LongSourceId, source.GuidSourceId, source.Item3);
                    restores.Add(Task.Run(() => _storage.RestoreProjections(envelop, _scope), stoppingToken));
                }
                Task.WaitAll(restores.ToArray(), stoppingToken);

                var c = checkpoint with { Sequence = checkpoint.Sequence + events.Count() };
                await _storage.SaveCheckpoint(checkpoint);
                Thread.Sleep(30000);
                _logger.LogInformation($"Restored projections for batch of {events.Count()} events.");
            }
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failure restoring projections.{Environment.NewLine} {e.StackTrace}.");
            throw;
        }
    }
    public async Task StartPolling(CancellationToken stoppingToken)
    {
        await _poll.PollAsync(stoppingToken);
        try
        {
            var x = _poll.QueuedProjections.Count;
            _logger.LogInformation($"Executing {x} pending projection task(s).");
            while(!stoppingToken.IsCancellationRequested)
            {
                var projectTask = _poll.DequeueAsync();
                if(projectTask == null)
                    break;
                await projectTask(_scope, stoppingToken);
                // save checkpoint
            }
            _logger.LogInformation($"{x} pending projection task(s) completed.");
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failure polling for projections. {Environment.NewLine} {e.StackTrace}.");
            throw;
        }
    }
}