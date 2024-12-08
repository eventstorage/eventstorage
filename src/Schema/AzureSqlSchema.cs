using System.Data;

namespace EventStorage.Schema;

public class AzureSqlSchema(string schema) : EventStorageSchema(schema)
{
    public override string CreateSchemaIfNotExists =>
        @$"IF SCHEMA_ID('{Schema}') IS NULL
        EXEC ('CREATE SCHEMA {Schema}')
        IF OBJECT_ID('{Schema}.EventSources') IS NULL
        CREATE TABLE [{Schema}].[EventSources](
            [{Sequence}] [bigint] IDENTITY(1,1) NOT NULL,
            [{Id}] [uniqueidentifier] NOT NULL,
            [{LongSourceId}] [bigint] NOT NULL,
            [{GuidSourceId}] [uniqueidentifier] NOT NULL,
            [{Version}] [int] NOT NULL,
            [{Type}] [nvarchar](255) NOT NULL,
            [{Data}] [json] NOT NULL,
            [{Timestamp}] [datetime] NOT NULL,
            [{SourceType}] [nvarchar](255) NOT NULL,
            [{CorrelationId}] [nvarchar](255) DEFAULT 'Default' NOT NULL,
            [{TenantId}] [nvarchar](255) DEFAULT 'Default' NOT NULL,
            [{CausationId}] [nvarchar](255) DEFAULT 'Default' NOT NULL,
            CONSTRAINT [PK_Sequence] PRIMARY KEY ([Sequence]),
            CONSTRAINT [AK_LongSourceId_Version] UNIQUE ([LongSourceId], [Version]),
            CONSTRAINT [AK_GuidSourceId_Version] UNIQUE ([GuidSourceId], [Version]));";
    protected override object[] EventStorageFieldTypes => [SqlDbType.UniqueIdentifier, SqlDbType.BigInt,
        SqlDbType.UniqueIdentifier, SqlDbType.Int, SqlDbType.Text, SqlDbType.NVarChar, SqlDbType.DateTime,
        SqlDbType.Text, SqlDbType.Text, SqlDbType.Text, SqlDbType.Text];
    protected override object[] ProjectionFieldTypes => [SqlDbType.BigInt, SqlDbType.UniqueIdentifier,
        SqlDbType.NVarChar, SqlDbType.NVarChar, SqlDbType.DateTime];
    public override string CreateProjectionIfNotExists(string projection) =>
        @$"IF OBJECT_ID('{Schema}.{projection}s') IS NULL
        CREATE TABLE [{Schema}].[{projection}s](
        [Id] [bigint] IDENTITY(1,1) NOT NULL,
        [LongSourceId] [bigint] NOT NULL,
        [GuidSourceId] [uniqueidentifier] NOT NULL,
        [Data] [json] NOT NULL,
        [Type] [nvarchar](255) NOT NULL,
        [UpdatedAt] [datetime] NOT NULL,
        CONSTRAINT [Pk_{projection}s_Id] PRIMARY KEY ([Id]),
        INDEX [IX_{projection}s_LongSourceId] NONCLUSTERED (LongSourceId),
        INDEX [IX_{projection}s_GuidSourceId] NONCLUSTERED (GuidSourceId))";
    public override string GetDocumentCommand<Td>(string sourceTId) => @$"SELECT TOP 1 * FROM
        {Schema}.{typeof(Td).Name}s WHERE {sourceTId} = @sourceId ORDER BY Id DESC";
    public override string CreateCheckpointIfNotExists =>
        @$"IF OBJECT_ID('{Schema}.Checkpoints') IS NULL
        CREATE TABLE [{Schema}].[Checkpoints](
        [Sequence] [bigint] NOT NULL,
        [Type] [tinyint] NOT NULL,
        [SourceType] [nvarchar](25) NOT NULL,
        INDEX [IX_Checkpoints_Sequence] NONCLUSTERED (Sequence))";
    public override string LoadEventsPastCheckpoint => @$"SELECT TOP 2 Sequence, LongSourceId,
    GuidSourceId, Data, Type FROM {Schema}.EventSources WHERE Sequence > @seq and Sequence <= @maxSeq";
}