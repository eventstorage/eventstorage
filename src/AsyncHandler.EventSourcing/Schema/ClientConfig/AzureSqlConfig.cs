namespace AsyncHandler.EventSourcing.Schema.ClientConfig;

public class AzureSqlConfig : ClientConfigBase
{
    public override string CreateIfNotExists => $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSources') " +
    "CREATE TABLE [dbo].[EventSources](" +
        $"[{EventSourceSchema.Sequence}] [bigint] IDENTITY(1,1) NOT NULL," +
        $"[{EventSourceSchema.Id}] [uniqueidentifier] NOT NULL," +
        $"[{EventSourceSchema.LongSourceId}] [bigint] NOT NULL," +
        $"[{EventSourceSchema.GuidSourceId}] [uniqueidentifier] NOT NULL," +
        $"[{EventSourceSchema.Version}] [bigint] NOT NULL," +
        $"[{EventSourceSchema.Type}] [nvarchar](255) NOT NULL," +
        $"[{EventSourceSchema.Data}] [json] NOT NULL," +
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL," +
        $"[{EventSourceSchema.SourceType}] [nvarchar](255) NOT NULL," +
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) DEFAULT 'Default' NOT NULL," +
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) DEFAULT 'Default' NOT NULL," +
        $"[{EventSourceSchema.CausationId}] [nvarchar](255) DEFAULT 'Default' NOT NULL," +
        $"CONSTRAINT [PK_Sequence] PRIMARY KEY ([Sequence])," +
        $"CONSTRAINT [AK_LongSourceId_Version] UNIQUE ([LongSourceId], [Version])," +
        // should be removed if performance affected
        $"CONSTRAINT [AK_GuidSourceId_Version] UNIQUE ([GuidSourceId], [Version])," +
    ");";
}