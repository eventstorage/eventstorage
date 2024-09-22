namespace AsyncHandler.EventSourcing.Schema;

public abstract class EventSourceSchema : IEventSourceSchema
{
    public static string Sequence => "Sequence";
    public static string Id => "Id";
    public static string LongSourceId => "LongSourceId";
    public static string GuidSourceId => "GuidSourceId";
    public static string Version => "Version";
    public static string Type => "Type";
    public static string Data => "Data";
    public static string Timestamp => "Timestamp";
    public static string SourceType => "SourceType";
    public static string CorrelationId => "CorrelationId";
    public static string TenantId => "TenantId";
    public static string CausationId => "CausationId";

    public abstract string CreateIfNotExists { get; }
    public virtual string GetSourceCommand(string sourceTId) =>
        @$"SELECT {LongSourceId}, {GuidSourceId}, {Type}, {Data} FROM [EventSources] WHERE [{sourceTId}] = @sourceId";
    public virtual string InsertSourceCommand => @"INSERT INTO [EventSources] VALUES ";
    public virtual string GetMaxSourceId =>
        @"SELECT T.LongSourceId FROM (SELECT MAX([LongSourceId]) as LongSourceId
        FROM [EventSources]) as T WHERE T.LongSourceId is not null;";
}