using System.Reflection;
using System.Text.Json;
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
    // these are loaded into a list later
    protected static Type ResolveEventType(string typeName)
    {
        var asses = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where assembly.FullName != null &&
                    !assembly.FullName.StartsWith("Microsoft") &&
                    !assembly.FullName.StartsWith("System")
                    select assembly
                    ).ToList();

        return asses.Where(x => x.GetReferencedAssemblies()
        .Any(an => AssemblyName.ReferenceMatchesDefinition(an, Assembly.GetExecutingAssembly().GetName())))
        .SelectMany(a => a.GetTypes())
        .FirstOrDefault(t => typeof(SourceEvent).IsAssignableFrom(t) && t.Name == typeName) ??
            throw new Exception($"Deserialize failure for event {typeName}, couldn't determine event type.");
    }
    private static IClientConfig GetClientConfig(IServiceProvider sp, EventSources source) =>
    sp.GetRequiredKeyedService<Dictionary<EventSources,IClientConfig>>("SourceConfig")
    .FirstOrDefault(x => x.Key == source).Value;
} 