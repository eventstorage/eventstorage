namespace EventStorage.Events;

public record EventEnvelop(long Seq, long LId, Guid GId, SourcedEvent SourcedEvent);
public record EventSourceEnvelop(long LId, Guid GId, IEnumerable<SourcedEvent> SourcedEvents);

public static class EnvelopExtensions
{
    public static EventSourceEnvelop Envelop(this IEnumerable<EventEnvelop> events) =>
        new(events.First().LId, events.First().GId, events.Select(e => e.SourcedEvent));
}