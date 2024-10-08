namespace EventStorage.Schema;

public interface IEventSourceSchema
{
    string[] SchemaFields { get; }
    Dictionary<string, object> Fields { get; }
    string CreateSchemaIfNotExists { get; }
    string GetSourceCommand(string sourceTId);
    string InsertSourceCommand { get; }
    string GetMaxSourceId { get; }
}