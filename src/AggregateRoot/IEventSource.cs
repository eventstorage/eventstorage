using EventStorage.Events;

namespace EventStorage.AggregateRoot;

public interface IEventSource
{
    IEnumerable<SourcedEvent> PendingEvents { get; }
    IEnumerable<SourcedEvent> EventStream { get; }
    void RestoreAggregate(bool stream, SourcedEvent[] events);
    IEnumerable<SourcedEvent> FlushPendingEvents();
}