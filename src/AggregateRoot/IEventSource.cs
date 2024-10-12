using EventStorage.Events;

namespace EventStorage.AggregateRoot;

public interface IEventSource
{
    IEnumerable<SourcedEvent> PendingEvents { get; }
    void RestoreAggregate(RestoreType restoration, SourcedEvent[] events);
    void CommitPendingEvents();
}