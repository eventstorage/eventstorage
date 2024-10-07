using System.Data;
using System.Data.Common;
using System.Text.Json;
using AsyncHandler.Asse;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;

namespace AsyncHandler.EventSourcing.Repositories;

public abstract class ClientBase<T>(IServiceProvider sp, EventSources source)
{
    private readonly IEventSourceSchema _schema = GetEventSourceSchema(sp, source);
    protected string GetSourceCommand => _schema.GetSourceCommand(SourceTId.ToString());
    protected string InsertSourceCommand => _schema.InsertSourceCommand;
    protected string CreateSchemaIfNotExists => _schema.CreateSchemaIfNotExists;
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

    // this needs optimistic locking
    protected async Task<string> GenerateSourceId(DbCommand command)
    {
        command.CommandText = GetMaxSourceId;
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        long longId = 0;
        if(reader.HasRows)
            longId = (long)reader.GetValue(0);
        LongSourceId = longId + 1;
        GuidSourceId = Guid.NewGuid();
        return SourceTId == TId.LongSourceId ? LongSourceId.ToString() : GuidSourceId.ToString();
    }
    protected void PrepareCommand(
        Func<Dictionary<string, object>, object[], int, DbParameter[]> parameters,
        DbCommand command, SourcedEvent[] events)
    {
        var sqlCommand = $"{InsertSourceCommand}";
        foreach (var (e, index) in events.Select((x,i) => (x,i)))
        {
            sqlCommand += "(";
            _schema.SchemaFields.ToList().ForEach(x => sqlCommand += x + index + ",");
            sqlCommand = sqlCommand[0..^1] + "),";

            object[] values = [e.Id, LongSourceId, GuidSourceId, e.Version,
                e.GetType().Name, JsonSerializer.Serialize(e, e.GetType(), SerializerOptions),
                DateTime.UtcNow, typeof(T).Name, e.TenantId?? "default", e.CorrelationId??
                "default", e.CausationId?? "default"];

            command.Parameters.AddRange(parameters(_schema.Fields, values, index));
        }
        command.CommandText = sqlCommand[0..^1];
    }
}
public enum TId
{
    LongSourceId,
    GuidSourceId,
}