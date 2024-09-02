using System.Reflection;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Schema;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase
{
    protected static string GetSourceCommand => @"SELECT * FROM [dbo].[EventSources] WHERE [SourceId] = @sourceId";
    protected string InsertSourceCommand = @"INSERT INTO [dbo].[EventSources] VALUES ";
    protected static string CreateIfNotExists => $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSources') "+
    "CREATE TABLE [dbo].[EventSources]("+
        $"[{EventSourceSchema.Sequence}] [bigint] IDENTITY(1,1) NOT NULL,"+
        $"[{EventSourceSchema.Id}] [uniqueidentifier] NOT NULL,"+
        $"[{EventSourceSchema.SourceId}] [bigint] NOT NULL,"+
        $"[{EventSourceSchema.Version}] [bigint] NOT NULL,"+
        $"[{EventSourceSchema.Type}] [nvarchar](255) NOT NULL,"+
        // data type is changed to json for Azure later
        $"[{EventSourceSchema.Data}] [nvarchar](4000) NOT NULL,"+
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL,"+
        $"[{EventSourceSchema.SourceType}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) DEFAULT 'Default' NOT NULL,"+
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) DEFAULT 'Default' NOT NULL,"+
        $"[{EventSourceSchema.CausationId}] [nvarchar](255) DEFAULT 'Default' NOT NULL,"+
        $"CONSTRAINT [PK_Sequence] PRIMARY KEY ([Sequence]),"+
        $"CONSTRAINT [AK_SourceId_Version] UNIQUE ([SourceId], [Version]),"+
    ");";
    // $"CREEATE INDEX Idx_SourceId ON [dbo].[EventSource] ([SourceId]);";
    public static string GetMaxSourceId => @"SELECT T.SourceId FROM (SELECT MAX([SourceId]) as SourceId FROM [dbo].[EventSources]) as T WHERE T.SourceId is not null;";
    // these are loaded into a list later
    protected static Type ResolveEventType(string typeName)
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
        .FirstOrDefault(t => typeof(SourceEvent).IsAssignableFrom(t) && t.Name == typeName) ??
            throw new Exception($"Deserialize failure for event {typeName}, couldn't determine event type.");
    }
} 