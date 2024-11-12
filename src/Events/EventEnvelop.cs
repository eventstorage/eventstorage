namespace EventStorage.Events;

public record EventEnvelop(long LongSourceId, Guid GuidSourceId, SourcedEvent SourcedEvent);