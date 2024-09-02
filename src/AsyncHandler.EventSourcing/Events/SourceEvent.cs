namespace AsyncHandler.EventSourcing.Events;

public record SourceEvent(
    string? Id = null,
    long Version = 0,
    string? Type = null,
    long? SourceId = null,
    string? SourceType = null,
    DateTime? Timestamp = null,
    string? TenantId = null,
    string? CorrelationId = null,
    string? CausationId = null
);