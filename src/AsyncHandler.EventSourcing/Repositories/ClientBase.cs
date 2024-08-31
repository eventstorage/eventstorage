using System.Reflection;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Schema;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase
{
    public static string GetSourceCommand => @"SELECT * FROM [dbo].[EventSource] WHERE [SourceId] = @sourceId";
    public static string InsertSourceCommand => @"INSERT INTO [dbo].[EventSource] VALUES (@id, @sourceId, @version, @type, @data, @timestamp, @sourceName, @correlationId, @tenantId)";
    public static string CreateIfNotExists => $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSource') "+
    "CREATE TABLE [dbo].[EventSource]("+
        $"[{EventSourceSchema.Sequence}] [bigint] IDENTITY(1,1) NOT NULL,"+
        $"[{EventSourceSchema.Id}] [uniqueidentifier] NOT NULL,"+
        $"[{EventSourceSchema.SourceId}] [bigint] NOT NULL,"+
        $"[{EventSourceSchema.Version}] [bigint] NOT NULL,"+
        $"[{EventSourceSchema.Type}] [nvarchar](255) NOT NULL,"+
        // data type is changed to json for Azure later
        $"[{EventSourceSchema.Data}] [nvarchar](4000) NOT NULL,"+
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL,"+
        $"[{EventSourceSchema.SourceName}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) NULL,"+
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) NULL,"+
        $"CONSTRAINT [PK_Sequence] PRIMARY KEY ([Sequence]),"+
        $"CONSTRAINT [AK_SourceId_Version] UNIQUE ([SourceId], [Version]),"+
    ");";
    // $"CREEATE INDEX Idx_SourceId ON [dbo].[EventSource] ([SourceId]);";
    public static string GetMaxSourceId => @"SELECT T.SourceId FROM (SELECT MAX([SourceId]) as SourceId FROM [dbo].[EventSource]) as T WHERE T.SourceId is not null;";
    public static Type? ResolveEventType(string typeName)
    {
        var asses = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where assembly.FullName != null &&
                    !assembly.FullName.StartsWith("Microsoft") &&
                    !assembly.FullName.StartsWith("System")
                    select assembly
                    ).ToList();

        return asses.Where(x => x.GetReferencedAssemblies()
        .Any(an => AssemblyName.ReferenceMatchesDefinition(an, Assembly.GetExecutingAssembly().GetName())))
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => typeof(SourceEvent).IsAssignableFrom(t) && t.Name == typeName);
    }
} 