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

    public string[] SchemaFields => ["@id", "@longSourceId", "@guidSourceId", "@version",
        "@type", "@data", "@timestamp", "@sourceType", "@tenantId" ,"@correlationId", "@causationId"];
    public Dictionary<string,object> Fields =>
        SchemaFields.Select((x, i) => (x, FieldTypes[i])).ToDictionary();
    protected abstract object[] FieldTypes { get; }
    public abstract string CreateSchemaIfNotExists { get; }
    public abstract string CreateProjectionIfNotExists(string projection);
    public virtual string GetSourceCommand(string sourceTId) =>
        @$"SELECT {LongSourceId}, {GuidSourceId}, {Type}, {Data} FROM {schema}.EventSources
        WHERE {sourceTId} = @sourceId";
    public virtual string InsertSourceCommand => @$"INSERT INTO {schema}.EventSources
    ({Id}, {LongSourceId}, {GuidSourceId}, {Version}, {Type}, {Data}, {Timestamp}, {SourceType},
    {CorrelationId}, {TenantId}, {CausationId}) VALUES";
    public virtual string ApplyProjectionCommand(string projection) => @$"INSERT INTO
    {schema}.{projection}s (LongSourceId, GuidSourceId, Data, Type, UpdatedAt) VALUES
    (@longSourceId, @guidSourceId, @data, @type, @updatedAt)";
    public virtual string GetMaxSourceId =>
        @$"SELECT T.LongSourceId FROM (SELECT MAX(LongSourceId) as LongSourceId
        FROM {schema}.EventSources) as T WHERE T.LongSourceId is not null;";
}