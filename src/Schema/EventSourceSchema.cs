using System.Data;

namespace EventStorage.Schema;

public abstract class EventSourceSchema(string schema) : IEventSourceSchema
{
    protected string Schema => schema;
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

    protected static string[] EventStorageFieldNames => ["@id", "@longSourceId", "@guidSourceId", "@version",
        "@type", "@data", "@timestamp", "@sourceType", "@tenantId" ,"@correlationId", "@causationId"];
    protected static string[] ProjectionFieldNames =>
        ["@longSourceId", "@guidSourceId", "@data", "@type", "@updatedAt"];
    public Dictionary<string, object> EventStorageFields =>
        EventStorageFieldNames.Select((x, i) => (x, EventStorageFieldTypes[i])).ToDictionary();
    public Dictionary<string, object> ProjectionFields =>
        ProjectionFieldNames.Select((x, i) => (x, ProjectionFieldTypes[i])).ToDictionary();
    protected abstract object[] EventStorageFieldTypes { get; }
    protected abstract object[] ProjectionFieldTypes { get; }
    public abstract string CreateSchemaIfNotExists { get; }
    public abstract string CreateProjectionIfNotExists(string projection);
    public virtual string GetSourceCommand(string sourceTId) => @$"SELECT {Sequence}, {LongSourceId},
        {GuidSourceId}, {Type}, {Data} FROM {schema}.EventSources WHERE {sourceTId} = @sourceId";
    public virtual string AddEventsCommand => @$"INSERT INTO {schema}.EventSources ({Id}, {LongSourceId},
        {GuidSourceId}, {Version}, {Type}, {Data}, {Timestamp}, {SourceType}, {CorrelationId}, {TenantId},
        {CausationId}) VALUES (@{Id}, @{LongSourceId}, @{GuidSourceId}, @{Version}, @{Type}, @{Data},
        @{Timestamp}, @{SourceType}, @{CorrelationId}, @{TenantId}, @{CausationId})";
    public virtual string AddProjectionsCommand(string projection) => @$"INSERT INTO
        {schema}.{projection}s (LongSourceId, GuidSourceId, Data, Type, UpdatedAt) VALUES
        (@longSourceId, @guidSourceId, @data, @type, @updatedAt)";
    public virtual string GetMaxSourceId =>
        @$"SELECT T.LongSourceId FROM (SELECT MAX(LongSourceId) as LongSourceId
        FROM {schema}.EventSources) as T WHERE T.LongSourceId is not null;";
    public virtual string GetMaxSequenceId => @$"SELECT T.Sequence FROM (SELECT MAX(Sequence)
        as Sequence FROM {schema}.EventSources) as T WHERE T.Sequence is not null";
    public abstract string GetDocumentCommand<Td>(string sourceTId);
    public abstract string CreateCheckpointIfNotExists { get; }
    public virtual string LoadCheckpointCommand => @$"SELECT * FROM {Schema}.Checkpoints
        WHERE Type=@type and SourceType=@sourceType";
    public virtual string SaveCheckpointCommand => @$"UPDATE {Schema}.Checkpoints
        SET Sequence=@sequence WHERE Type=@type and SourceType=@sourceType";
    public virtual string InsertCheckpointCommand => @$"INSERT INTO {Schema}.Checkpoints
        (Sequence, Type, SourceType) VALUES (@sequence, @type, @sourceType)";
    public abstract string LoadEventsPastCheckpoint { get; }
}