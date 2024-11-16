namespace EventStorage.Events;

public record EventEnvelop(long LongSourceId, Guid GuidSourceId, SourcedEvent SourcedEvent);
public record EventSourceEnvelop(
    long LongSourceId,
    Guid GuidSourceId,
    IEnumerable<SourcedEvent> SourcedEvents);