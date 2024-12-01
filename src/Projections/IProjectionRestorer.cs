using EventStorage.AggregateRoot;
using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjectionRestorer<T> where T : IEventSource
{
    object? Project(IProjection<T> projection, IEnumerable<SourcedEvent> events, Type model);
    M? Project<M>(IEnumerable<SourcedEvent> events);
    bool Subscribes(IEnumerable<SourcedEvent> events, IProjection<T> projection);
}