namespace AsyncHandler.EventSourcing.SourceConfig;

public interface IClientConfig
{
    string CreateIfNotExists { get; }
    string GetSourceCommand { get; }
    string InsertSourceCommand { get; }
    string GetMaxSourceId { get; }
}