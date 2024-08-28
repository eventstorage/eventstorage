using AsyncHandler.EventSourcing.Schema;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase
{
    public static string GetSourceCommand => @"SELECT * FROM [dbo].[EventSource] WHERE [AggregateId] = @AggregateId";
    public static string InsertSourceCommand => @"INSERT INTO [dbo].[EventSource] VALUES (@timeStamp, @sequenceNumber, @aggregateId, @aggregateType, @version, @eventType, @data, @correlationId, @tenantId)";
    public static string CreateIfNotExists => $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSource') "+
    "CREATE TABLE [dbo].[EventSource]("+
        $"[{EventSourceSchema.Id}] [bigint] IDENTITY(1,1) NOT NULL,"+
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL,"+
        $"[{EventSourceSchema.SequenceNumber}] [int] NOT NULL,"+
        $"[{EventSourceSchema.AggregateId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.AggregateType}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.Version}] [int] NOT NULL,"+
        $"[{EventSourceSchema.EventType}] [nvarchar](255) NOT NULL,"+
        // data type is changed to json for Azure later
        $"[{EventSourceSchema.Data}] [nvarchar](4000) NOT NULL,"+
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) NOT NULL,"+
    ")";
} 