namespace AsyncHandler.EventSourcing.Schema;

public interface IEventSourceSchema
{
    string CreateIfNotExists { get; }
    string GetSourceCommand(string sourceTId);
    string InsertSourceCommand { get; }
    string GetMaxSourceId { get; }
}