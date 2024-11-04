using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjectionEngine
{
    object? Project(IProjection projection, IEnumerable<SourcedEvent> events, Type model);
    object? ProjectOptimized(IProjection projection, IEnumerable<SourcedEvent> events, Type model);
    M? Project<M>(IEnumerable<SourcedEvent> events);
    M? ProjectOptimized<M>(IEnumerable<SourcedEvent> events);
    bool Subscribes(IEnumerable<SourcedEvent> events, IProjection projection);
}