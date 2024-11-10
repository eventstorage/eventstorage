namespace EventStorage.Schema;

public interface IEventSourceSchema
{
    string[] SchemaFields { get; }
    Dictionary<string, object> Fields { get; }
    string CreateSchemaIfNotExists { get; }
    string CreateProjectionIfNotExists(string projection);
    string GetSourceCommand(string sourceTId);
    string InsertSourceCommand { get; }
    string ApplyProjectionCommand(string projection);
    string GetMaxSourceId { get; }
    string GetDocumentCommand<Td>(string sourceTId);
    string CreateCheckpointIfNotExists { get; }
    string LoadCheckpointCommand { get; }
    string SaveCheckpointCommand { get; }
    string LoadEventsPastSeq { get; }
}