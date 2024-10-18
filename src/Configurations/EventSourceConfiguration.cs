using EventStorage.AggregateRoot;
using EventStorage.Repositories;
using EventStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventStorage.Configurations;

public class EventSourceConfiguration(IServiceCollection services, string schema) 
    : EventStorageConfiguration(services, schema)
{
    public string ConnectionString { get; set; } = string.Empty;
    public EventStore Source { get; set; }
    public void InitSource()
    {
        // initialize source while app spins up
        ServiceCollection.AddSingleton<IHostedService>((sp) =>
        {
            var repository = new Repository<IEventSource>(ConnectionString, sp, Source);
            return new SourceInitializer(new EventStorage<IEventSource>(repository, Source));
        });
    }
}