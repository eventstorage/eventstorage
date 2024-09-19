using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing;

public interface IAggregateRoot
{
    IEnumerable<SourcedEvent> PendingEvents { get; }
    void RestoreAggregate(RestoreType restoration, SourcedEvent[] events);
    void CommitPendingEvents();
}