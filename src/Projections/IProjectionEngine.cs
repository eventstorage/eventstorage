using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjectionEngine
{
    object? Project(Type model, IEnumerable<SourcedEvent> events);
    object? ProjectOptimized(Type model, IEnumerable<SourcedEvent> events);
    M? Project<M>(IEnumerable<SourcedEvent> events);
    M? ProjectOptimized<M>(IEnumerable<SourcedEvent> events);
    // bool Subscribes(IEnumerable<SourcedEvent> events, IProjection projection);
}