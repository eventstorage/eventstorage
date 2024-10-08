using System.Data;

namespace EventStorage.Schema;

public class AzureSqlSchema(string schema) : EventSourceSchema(schema)
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
            CONSTRAINT [AK_GuidSourceId_Version] UNIQUE ([GuidSourceId], [Version]),
        );";
    protected override object[] FieldTypes =>
        [SqlDbType.UniqueIdentifier, SqlDbType.BigInt, SqlDbType.UniqueIdentifier,
        SqlDbType.Int, SqlDbType.Text, SqlDbType.NVarChar, SqlDbType.DateTime,
        SqlDbType.Text, SqlDbType.Text, SqlDbType.Text, SqlDbType.Text];
}