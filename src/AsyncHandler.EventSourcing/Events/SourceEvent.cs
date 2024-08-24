namespace AsyncHandler.EventSourcing.Events;

public record SourceEvent(
    string? EventId = null,
    DateTime? Timestamp = null,
    int Version = 0,
    string? SourceId = null,
    string? SourceType = null,
    string? TenantId = null,
    string? CorrelationId = null
);