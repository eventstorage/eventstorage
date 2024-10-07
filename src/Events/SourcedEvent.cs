namespace AsyncHandler.EventSourcing.Events;

public record SourcedEvent(string? CorrelationId = null)
{
    public Guid Id { get; set; }
    public long Version { get; set; }
    public string? Type { get; set; }
    public object? SourceId { get; set; }
    public string? SourceType { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CausationId { get; set; }
    public string? TenantId { get; set; }
};