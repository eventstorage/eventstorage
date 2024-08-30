using AsyncHandler.EventSourcing.Schema;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase
{
    public static string GetSourceCommand => @"SELECT * FROM [dbo].[EventSource] WHERE [SourceId] = @sourceId";
    public static string InsertSourceCommand => @"INSERT INTO [dbo].[EventSource] VALUES (@id, @sourceId, @version, @type, @data, @timestamp, @correlationId, @tenantId)";
    public static string CreateIfNotExists => $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSource') "+
    "CREATE TABLE [dbo].[EventSource]("+
        $"[{EventSourceSchema.Sequence}] [bigint] IDENTITY(1,1) NOT NULL,"+
        $"[{EventSourceSchema.Id}] [uniqueidentifier] NOT NULL,"+
        $"[{EventSourceSchema.SourceId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.Version}] [bigint] NOT NULL,"+
        $"[{EventSourceSchema.Type}] [nvarchar](255) NOT NULL,"+
        // data type is changed to json for Azure later
        $"[{EventSourceSchema.Data}] [nvarchar](4000) NOT NULL,"+
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL,"+
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) NULL,"+
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) NULL,"+
        $"CONSTRAINT [PK_Sequence] PRIMARY KEY ([Sequence]),"+
        $"CONSTRAINT [AK_SourceId_Version] UNIQUE ([SourceId], [Version]),"+
    ");";
    // $"CREEATE INDEX Idx_SourceId ON [dbo].[EventSource] ([SourceId]);";
} 