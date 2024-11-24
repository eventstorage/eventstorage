namespace EventStorage.Schema;

public interface IEventStorageSchema
{
    string CreateSchemaIfNotExists { get; }
    string CreateProjectionIfNotExists(string projection);
    string LoadEventSourceCommand(string sourceTId);
    string AddEventsCommand { get; }
    string AddProjectionsCommand(string projection);
    Dictionary<string, object> EventStorageFields { get; }
    Dictionary<string, object> ProjectionFields { get; }
    string GetMaxSourceId { get; }
    string GetMaxSequenceId { get; }
    string GetDocumentCommand<Td>(string sourceTId);
    string CreateCheckpointIfNotExists { get; }
    string LoadCheckpointCommand { get; }
    string SaveCheckpointCommand { get; }
    string InsertCheckpointCommand { get; }
    string LoadEventsPastCheckpoint { get; }
}