using NpgsqlTypes;
namespace EventStorage.Schema;

public class PostgreSqlSchema(string schema) : EventSourceSchema(schema)
{
    public override string CreateSchemaIfNotExists =>
        @$"CREATE SCHEMA IF NOT EXISTS {Schema};
        CREATE TABLE IF NOT EXISTS {Schema}.EventSources(
            {Sequence} bigint NOT NULL generated always as identity,
            {Id} uuid NOT NULL,
            {LongSourceId} bigint NOT NULL,
            {GuidSourceId} uuid NOT NULL,
            {Version} int NOT NULL,
            {Type} text NOT NULL,
            {Data} jsonb NOT NULL,
            {Timestamp} timestamptz NOT NULL DEFAULT now(),
            {SourceType} text NOT NULL,
            {CorrelationId} text NOT NULL DEFAULT 'Default',
            {TenantId} text NOT NULL DEFAULT 'Default',
            {CausationId} text NOT NULL DEFAULT 'Default',
            CONSTRAINT PK_Sequence PRIMARY KEY (Sequence),
            CONSTRAINT UK_LongSourceId_Version UNIQUE (LongSourceId, Version),
            CONSTRAINT UK_GuidSourceId_Version UNIQUE (GuidSourceId, Version)
        );
        CREATE INDEX IF NOT EXISTS Idx_EventSources_LongSourceId on {Schema}.EventSources (LongSourceId);
        CREATE INDEX IF NOT EXISTS Idx_EventSources_GuidSourceId on {Schema}.EventSources (GuidSourceId);";
    protected override object[] FieldTypes =>
        [NpgsqlDbType.Uuid, NpgsqlDbType.Bigint, NpgsqlDbType.Uuid,
        NpgsqlDbType.Integer, NpgsqlDbType.Text, NpgsqlDbType.Jsonb, NpgsqlDbType.TimestampTz,
        NpgsqlDbType.Text, NpgsqlDbType.Text, NpgsqlDbType.Text, NpgsqlDbType.Text];
    public override string CreateProjectionIfNotExists(string projection) =>
        @$"CREATE TABLE IF NOT EXISTS {Schema}.{projection}s(
        Id bigint NOT NULL generated always as identity,
        LongSourceId bigint NOT NULL,
        GuidSourceId uuid NOT NULL,
        Data jsonb NOT NULL,
        Type text NOT NULL,
        UpdatedAt timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT Pk_{projection}s_Id PRIMARY KEY (Id));";
    public override string GetDocumentCommand<Td>(string sourceTId) => @$"SELECT * FROM
        {Schema}.{typeof(Td).Name}s WHERE {sourceTId} = @sourceId ORDER BY Id DESC LIMIT 1";
    public override string CreateCheckpointIfNotExists =>
        @$"IF OBJECT_ID('{Schema}.Checkpoints') IS NULL
        CREATE TABLE [{Schema}].[Checkpoints](
        [Sequence] [bigint] NOT NULL,
        [Type] [tinyint] NOT NULL,
        [SourceType] [nvarchar](25) NOT NULL,
        CONSTRAINT [Pk_Checkpoints_Sequence] PRIMARY KEY ([Sequence]))";
    public override string LoadEventsPastCheckpoint => @$"SELECT Sequence, LongSourceId, GuidSourceId,
        Data, Type FROM {Schema}.EventSources WHERE Sequence > @sequence LIMIT 3";
}