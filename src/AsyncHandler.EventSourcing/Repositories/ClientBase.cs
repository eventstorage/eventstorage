using System.Reflection;
using System.Text.Json;
using AsyncHandler.Asse;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.SourceConfig;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase<T>(IServiceProvider sp, EventSources source)
{
    private readonly IClientConfig _config = GetClientConfig(sp, source);
    protected string GetSourceCommand => _config.GetSourceCommand(SourceTId.ToString());
    protected string InsertSourceCommand => _config.InsertSourceCommand;
    protected string CreateIfNotExists => _config.CreateIfNotExists;
    public string GetMaxSourceId => _config.GetMaxSourceId;
    public static JsonSerializerOptions SerializerOptions => new() { IncludeFields = true };
    
    protected long LongSourceId { get; set; } = 1;
    protected Guid GuidSourceId { get; set; } = Guid.NewGuid();
    private static Type? _genericTypeArg => typeof(T).BaseType?.GenericTypeArguments[0];
    protected static TId SourceTId => _genericTypeArg != null && 
        _genericTypeArg.IsAssignableFrom(typeof(long)) ? TId.LongSourceId : TId.GuidSourceId;
    
    protected static Type ResolveEventType(string typeName) =>
        TDiscover.FindByTypeName<SourcedEvent>(typeName) ??
        throw new Exception($"Deserialize failure for event {typeName}, couldn't determine event type.");
    
    private static IClientConfig GetClientConfig(IServiceProvider sp, EventSources source) =>
    sp.GetRequiredKeyedService<Dictionary<EventSources,IClientConfig>>("SourceConfig")
    .FirstOrDefault(x => x.Key == source).Value;
}
public enum TId
{
    LongSourceId,
    GuidSourceId,
}