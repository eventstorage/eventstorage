namespace EventStorage.Events;

public record EventEnvelop(long Seq, long LId, Guid GId, SourcedEvent SourcedEvent);
public record EventSourceEnvelop(
    long LId,
    Guid GId,
    IEnumerable<SourcedEvent> SourcedEvents);