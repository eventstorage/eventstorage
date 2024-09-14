using System.Reflection;
using System.Text.Json;
using AsyncHandler.Asse;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.SourceConfig;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase(IServiceProvider sp, EventSources source)
{
    private readonly IClientConfig _config = GetClientConfig(sp, source);
    protected string GetSourceCommand => _config.GetSourceCommand;
    protected string InsertSourceCommand => _config.InsertSourceCommand;
    protected string CreateIfNotExists => _config.CreateIfNotExists;
    public string GetMaxSourceId => _config.GetMaxSourceId;
    public static JsonSerializerOptions SerializerOptions => new() { IncludeFields = true };
    // these could be loaded into a list
    protected static Type ResolveEventType(string typeName) =>
        TDiscover.FindByTypeName<SourceEvent>(typeName) ??
        throw new Exception($"Deserialize failure for event {typeName}, couldn't determine event type.");
    
    private static IClientConfig GetClientConfig(IServiceProvider sp, EventSources source) =>
    sp.GetRequiredKeyedService<Dictionary<EventSources,IClientConfig>>("SourceConfig")
    .FirstOrDefault(x => x.Key == source).Value;
} 