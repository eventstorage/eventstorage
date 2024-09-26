using System.Text.Json;
using AsyncHandler.Asse;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase<T>(IServiceProvider sp, EventSources source)
{
    private readonly IEventSourceSchema _schema = GetEventSourceSchema(sp, source);
    protected string GetSourceCommand => _schema.GetSourceCommand(SourceTId.ToString());
    protected string InsertSourceCommand => _schema.InsertSourceCommand;
    protected string CreateIfNotExists => _schema.CreateIfNotExists;
    public string GetMaxSourceId => _schema.GetMaxSourceId;
    public static JsonSerializerOptions SerializerOptions => new() { IncludeFields = true };
    
    protected long LongSourceId { get; set; } = 1;
    protected Guid GuidSourceId { get; set; } = Guid.NewGuid();
    private static Type? _genericTypeArg => typeof(T).BaseType?.GenericTypeArguments[0];
    protected static TId SourceTId => _genericTypeArg != null && 
        _genericTypeArg.IsAssignableFrom(typeof(long)) ? TId.LongSourceId : TId.GuidSourceId;
    
    protected static Type ResolveEventType(string typeName) =>
        TDiscover.FindByTypeName<SourcedEvent>(typeName) ??
        throw new Exception($"Deserialize failure for event {typeName}, couldn't determine event type.");
    
    private static IEventSourceSchema GetEventSourceSchema(IServiceProvider sp, EventSources source) =>
    sp.GetRequiredKeyedService<Dictionary<EventSources,IEventSourceSchema>>("Schema")
    .FirstOrDefault(x => x.Key == source).Value;
}
public enum TId
{
    LongSourceId,
    GuidSourceId,
}