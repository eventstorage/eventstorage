using AsyncHandler.EventSourcing.Schema;

namespace AsyncHandler.EventSourcing.SourceConfig;

public abstract class ClientConfigBase : IClientConfig
{
    public abstract string CreateIfNotExists { get; }
    public virtual string GetSourceCommand(string sourceTId) => @$"SELECT {EventSourceSchema.LongSourceId}, {EventSourceSchema.GuidSourceId}, {EventSourceSchema.Type}, {EventSourceSchema.Data} FROM [dbo].[EventSources] WHERE [{sourceTId}] = @sourceId";
    public virtual string InsertSourceCommand => @"INSERT INTO [dbo].[EventSources] VALUES ";
    public virtual string GetMaxSourceId => @"SELECT T.LongSourceId FROM (SELECT MAX([LongSourceId]) as LongSourceId FROM [dbo].[EventSources]) as T WHERE T.LongSourceId is not null;";
}