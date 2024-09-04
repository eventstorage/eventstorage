namespace AsyncHandler.EventSourcing.SourceConfig;

public abstract class ClientConfigBase : IClientConfig
{
    public abstract string CreateIfNotExists { get; }
    public virtual string GetSourceCommand => @"SELECT * FROM [dbo].[EventSources] WHERE [SourceId] = @sourceId";
    public virtual string InsertSourceCommand => @"INSERT INTO [dbo].[EventSources] VALUES ";
    public virtual string GetMaxSourceId => @"SELECT T.SourceId FROM (SELECT MAX([SourceId]) as SourceId FROM [dbo].[EventSources]) as T WHERE T.SourceId is not null;";
}