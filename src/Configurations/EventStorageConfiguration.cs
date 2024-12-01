using EventStorage.AggregateRoot;
using EventStorage.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Configurations;

public abstract class EventStorageConfiguration(
    IServiceCollection services, string? schema, string? conn) : ConfigurationBase
{
    internal IServiceCollection ServiceCollection = services;
    internal IServiceProvider Sp => ServiceCollection.BuildServiceProvider();
    public virtual string Schema { get; set; } = schema?? "es";
    public virtual string? ConnectionString { get; set; } = conn;
    internal EventStore Store { get; set; }
    internal abstract List<Projection> Projections { get; set; }
}