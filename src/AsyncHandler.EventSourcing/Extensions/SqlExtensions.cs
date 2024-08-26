using System.Reflection;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Data.SqlClient;

namespace AsyncHandler.EventSourcing.Extensions;

public static class SqlExtensions
{
    private static readonly string createIfNotExists = $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSource') "+
    "CREATE TABLE [dbo].[EventSource]("+
        $"[{EventSourceSchema.Id}] [bigint] IDENTITY(1,1) NOT NULL,"+
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL,"+
        $"[{EventSourceSchema.SequenceNumber}] [int] NOT NULL,"+
        $"[{EventSourceSchema.AggregateId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.AggregateType}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.Version}] [int] NOT NULL,"+
        $"[{EventSourceSchema.EventType}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.Data}] [nvarchar](4000) NOT NULL,"+
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) NOT NULL,"+
    ")";
    public static void InitConnection(this SqlConnection sqlConnection)
    {
        sqlConnection.Open();
        SqlCommand command = new(createIfNotExists, sqlConnection);
        command.ExecuteNonQuery();
    }
}