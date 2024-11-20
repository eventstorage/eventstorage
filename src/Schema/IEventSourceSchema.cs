namespace EventStorage.Schema;

public interface IEventSourceSchema
{
    Dictionary<string, object> EventStorageFields { get; }
    string CreateSchemaIfNotExists { get; }
    string CreateProjectionIfNotExists(string projection);
    string GetSourceCommand(string sourceTId);
    string AddEventsCommand { get; }
    string AddProjectionsCommand(string projection);
    string GetMaxSourceId { get; }
    string GetMaxSequenceId { get; }
    string GetDocumentCommand<Td>(string sourceTId);
    string CreateCheckpointIfNotExists { get; }
    string LoadCheckpointCommand { get; }
    string SaveCheckpointCommand { get; }
    string InsertCheckpointCommand { get; }
    string LoadEventsPastCheckpoint { get; }
}