namespace AsyncHandler.EventSourcing.Schema;

public static class EventSourceSchema
{
    public static string Id => "Id";
    public static string Timestamp => "Timestamp";
    public static string SequenceNumber => "SequenceNumber";
    public static string AggregateId => "AggregateId";
    public static string AggregateType => "AggregateType";
    public static string Version => "Version";
    public static string EventType => "EventType";
    public static string Data => "Data";
    public static string CorrelationId => "CorrelationId";
    public static string TenantId => "TenantId";
}