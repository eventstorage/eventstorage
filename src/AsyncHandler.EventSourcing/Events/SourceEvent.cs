namespace AsyncHandler.EventSourcing.Events;

public record SourceEvent(
    string? EventId = null,
    DateTime? Timestamp = null,
    long Version = 0,
    long? SourceId = null,
    string? SourceType = null,
    string? TenantId = null,
    string? CorrelationId = null
);