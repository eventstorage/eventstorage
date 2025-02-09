using NpgsqlTypes;
namespace EventStorage.Schema;

public class PostgreSqlSchema(string schema) : EventStorageSchema(schema)
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
    protected override object[] EventStorageFieldTypes => [NpgsqlDbType.Uuid, NpgsqlDbType.Bigint,
        NpgsqlDbType.Uuid, NpgsqlDbType.Integer, NpgsqlDbType.Text, NpgsqlDbType.Jsonb, NpgsqlDbType.TimestampTz,
        NpgsqlDbType.Text, NpgsqlDbType.Text, NpgsqlDbType.Text, NpgsqlDbType.Text];
    protected override object[] ProjectionFieldTypes => [NpgsqlDbType.Bigint, NpgsqlDbType.Uuid,
        NpgsqlDbType.Jsonb, NpgsqlDbType.Text, NpgsqlDbType.TimestampTz];
    public override string CreateProjectionIfNotExists(string projection) =>
        @$"CREATE TABLE IF NOT EXISTS {Schema}.{projection}s(
            Id bigint NOT NULL generated always as identity,
            LongSourceId bigint NOT NULL,
            GuidSourceId uuid NOT NULL,
            Data jsonb NOT NULL,
            Type text NOT NULL,
            UpdatedAt timestamptz NOT NULL DEFAULT now(),
            CONSTRAINT Pk_{projection}s_Id PRIMARY KEY (Id)
        );
        CREATE INDEX IF NOT EXISTS Idx_{projection}s_LongSourceId on {Schema}.{projection}s (LongSourceId);
        CREATE INDEX IF NOT EXISTS Idx_{projection}s_GuidSourceId on {Schema}.{projection}s (GuidSourceId);";
    public override string GetDocumentCommand<Td>(string sourceTId) => @$"SELECT * FROM
        {Schema}.{typeof(Td).Name}s WHERE {sourceTId} = @sourceId ORDER BY Id DESC LIMIT 1";
    public override string CreateCheckpointIfNotExists =>
        @$"CREATE TABLE IF NOT EXISTS {Schema}.Checkpoints(
            Id smallint NOT NULL generated always as identity,
            Subscription text NOT NULL,
            Type smallint NOT NULL,
            Sequence bigint NOT NULL,
            CONSTRAINT Pk_Checkpoints_Id PRIMARY KEY (Id)
        );
        CREATE INDEX IF NOT EXISTS Idx_Checkpoints_Subscription on {Schema}.Checkpoints (Subscription);";
    public override string LoadEventsPastCheckpoint => @$"SELECT Sequence, LongSourceId, GuidSourceId,
        Data, Type FROM {Schema}.EventSources WHERE Sequence > @seq and Sequence <= @maxSeq
        ORDER BY Sequence ASC LIMIT 25";
    public override string CreateConcurrencyCheckFunction =>
        @$"CREATE OR REPLACE FUNCTION {Schema}.check_concurrency(source_id bigint, expected integer)
        RETURNS VOID AS $$
        DECLARE current int;
        BEGIN
            SELECT MAX(Version) INTO current FROM {Schema}.EventSources WHERE LongSourceId=source_id;
            IF (expected IS NULL AND current IS NOT NULL) OR (current IS NOT NULL AND current != expected)
                THEN RAISE EXCEPTION 'concurrent stream access detected. source_id %, expected %, current %',
                source_id, expected, current;
            END IF;
        END;
        $$ language plpgsql;";
    public override string CheckConcurrency => @$"SELECT {Schema}.check_concurrency(@sourceId, @expected);";
    
}