namespace AsyncHandler.EventSourcing.Schema;

public static class EventSourceSchema
{
    public static string Sequence => "Sequence";
    public static string Id => "Id";
    public static string SourceId => "SourceId";
    public static string Version => "Version";
    public static string Type => "Type";
    public static string Data => "Data";
    public static string Timestamp => "Timestamp";
    public static string SourceName => "SourceName";
    public static string CorrelationId => "CorrelationId";
    public static string TenantId => "TenantId";
}