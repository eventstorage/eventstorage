namespace AsyncHandler.EventSourcing.Schema;

public interface IEventSourceSchema
{
    string CreateSchemaIfNotExists { get; }
    string GetSourceCommand(string sourceTId);
    string InsertSourceCommand { get; }
    string GetMaxSourceId { get; }
}