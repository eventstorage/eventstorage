using AsyncHandler.EventSourcing.Configuration;
using Microsoft.Extensions.Hosting;

namespace AsyncHandler.EventSourcing.Workers;

public class SourceInitializer(IRepository<AggregateRoot> repository, EventSources source) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Run( () => source switch
        {
            EventSources.AzureSql => repository.AzureSqlClient.InitSource(),
            EventSources.PostgresSql => repository.PostgreSqlClient.InitSource(),
            EventSources.SQLServer => repository.SqlServerClient.InitSource(),
            _ => Task.CompletedTask,
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
