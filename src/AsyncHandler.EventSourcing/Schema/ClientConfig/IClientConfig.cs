namespace AsyncHandler.EventSourcing.Schema.ClientConfig;

public interface IClientConfig
{
    string CreateIfNotExists { get; }
    string GetSourceCommand(string sourceTId);
    string InsertSourceCommand { get; }
    string GetMaxSourceId { get; }
}