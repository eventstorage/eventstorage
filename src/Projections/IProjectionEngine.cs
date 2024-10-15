using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjectionEngine
{
    M Project<M>(IEnumerable<SourcedEvent> events) where M : class;
}