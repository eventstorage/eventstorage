using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjectionEngine
{
    object? Project(Type type, IEnumerable<SourcedEvent> events);
    M? Project<M>(IEnumerable<SourcedEvent> events);
}