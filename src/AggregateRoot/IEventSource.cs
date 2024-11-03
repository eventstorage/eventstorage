using EventStorage.Events;

namespace EventStorage.AggregateRoot;

public interface IEventSource
{
    IEnumerable<SourcedEvent> PendingEvents { get; }
    IEnumerable<SourcedEvent> EventStream { get; }
    void RestoreAggregate(RestoreType restoration, SourcedEvent[] events);
    IEnumerable<SourcedEvent> CommitPendingEvents();
}