using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Repositories;
using EventStorage.Repositories.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pipelines.Sockets.Unofficial.Arenas;
using TDiscover;

namespace EventStorage.Workers;

public class AsyncProjectionEngine(
    EventStorage<IEventSource> storage, IServiceProvider sp) : BackgroundService
{
    private readonly ILogger _logger = sp.GetRequiredService<ILogger<AsyncProjectionEngine>>();
    private readonly IAsyncProjectionPoll _poll = sp.GetRequiredService<IAsyncProjectionPoll>();
    private readonly IEnumerable<IProjection> _projections = sp.GetServices<IProjection>();
    private readonly IRedisService _redis = sp.GetRequiredService<IRedisService>();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started projection engine, restoring projections...");
        await StartPolling(stoppingToken);
        // var checkpoint = await RestoreProjections(stoppingToken);
        // await storage.SaveCheckpoint(checkpoint);
        _logger.LogInformation("Restoration completed and saved checkpoint.");
    }
    public async Task<Checkpoint> RestoreProjections(CancellationToken stoppingToken)
    {
        var checkpoint = await storage.LoadCheckpoint();
        while(!stoppingToken.IsCancellationRequested)
        {
            var events = await storage.LoadEventsPastCheckpoint(checkpoint);
            if(!events.Any())
                break;
            var groupedBySource = (from e in events 
                                group e by e.LongSourceId into groupedById
                                select groupedById).Select(x => (x.First().LongSourceId,
                                x.First().GuidSourceId, x.Select(x => x.SourcedEvent)));

            List<Task> restores = [];
            foreach (var eventSource in groupedBySource)
            {
                EventSourceEnvelop envelop = new(eventSource.LongSourceId,
                    eventSource.GuidSourceId,
                    eventSource.Item3);
                restores.Add(Task.Run(() => _redis.RestoreProjections(envelop), stoppingToken));
                restores.Add(Task.Run(() => storage.RestoreProjections(envelop), stoppingToken));
            }
            Task.WaitAll(restores.ToArray(), stoppingToken);
        }
        return checkpoint;
    }
    private async Task StartPolling(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await _poll.PollAsync(stoppingToken);
        }
    }
}