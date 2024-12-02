using EventStorage.AggregateRoot;
using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjectionRestorer
{
    object? Project(IProjection projection, IEnumerable<SourcedEvent> events, Type model);
    M? Project<M>(IEnumerable<SourcedEvent> events);
    bool Subscribes(IEnumerable<SourcedEvent> events, IProjection projection);
}