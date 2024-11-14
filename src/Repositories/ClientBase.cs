using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.Configurations;
using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Repositories.Redis;
using EventStorage.Schema;
using EventStorage.Workers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using TDiscover;

namespace EventStorage.Repositories;

public abstract class ClientBase<T>(IServiceProvider sp, EventStore source)
{
    public IServiceProvider Sp => sp;
    private readonly IEventSourceSchema _schema = GetEventSourceSchema(sp, source);
    protected string GetSourceCommand => _schema.GetSourceCommand(SourceTId.ToString());
    protected string GetDocumentCommand<Td>() => _schema.GetDocumentCommand<Td>(SourceTId.ToString());
    protected string InsertSourceCommand => _schema.InsertSourceCommand;
    protected string CreateSchemaIfNotExists => _schema.CreateSchemaIfNotExists;
    protected string CreateProjectionIfNotExists(string projection) =>
        _schema.CreateProjectionIfNotExists(projection);
    protected string ApplyProjectionCommand(string projection) => _schema.ApplyProjectionCommand(projection);
    protected string GetMaxSourceId => _schema.GetMaxSourceId;
    protected string CreateCheckpointIfNotExists => _schema.CreateCheckpointIfNotExists;
    protected string LoadCheckpointCommand => _schema.LoadCheckpointCommand;
    protected string SaveCheckpointCommand => _schema.SaveCheckpointCommand;
    protected string LoadEventsPastCheckpointCommand => _schema.LoadEventsPastCheckpoint;
    protected static JsonSerializerOptions SerializerOptions => new() { IncludeFields = true };

    protected long LongSourceId { get; set; } = 1;
    protected Guid GuidSourceId { get; set; } = Guid.NewGuid();
    private static readonly Type? _genericTypeArg = typeof(T).BaseType?.GenericTypeArguments[0];
    protected static TId SourceTId => _genericTypeArg != null &&
        _genericTypeArg.IsAssignableFrom(typeof(long)) ? TId.LongSourceId : TId.GuidSourceId;

    protected static Type ResolveEventType(string typeName) =>
        Td.FindByTypeName<SourcedEvent>(typeName) ??
        throw new Exception($"Deserialize failure for event {typeName}, couldn't determine event type.");

    private static IEventSourceSchema GetEventSourceSchema(IServiceProvider sp, EventStore source) =>
        sp.GetRequiredKeyedService<Dictionary<EventStore, IEventSourceSchema>>("Schema")
        .FirstOrDefault(x => x.Key == source).Value;

    protected IRedisService Redis => Sp.GetRequiredService<IRedisService>();
    protected IProjectionRestorer ProjectionRestorer => sp.GetRequiredService<IProjectionRestorer>();
    protected IAsyncProjectionPoll ProjectionPoll => sp.GetRequiredService<IAsyncProjectionPoll>();
    protected IEnumerable<IProjection> Projections => Sp.GetServices<IProjection>();
    #pragma warning disable CS8619
    protected IEnumerable<Type> TProjections(Func<IProjection, bool> predicate) =>
        Projections.Where(predicate)
        .Where(p => p.Mode != ProjectionMode.Runtime)
        .Where(p => p.Configuration.Store == ProjectionStore.Selected)
        .Select(p => p.GetType().BaseType?.GenericTypeArguments.First());

    // this needs optimistic locking
    protected async Task<string> GenerateSourceId(DbCommand command)
    {
        command.CommandText = GetMaxSourceId;
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        long longId = 0;
        if (reader.HasRows)
            longId = (long)reader.GetValue(0);
        LongSourceId = longId + 1;
        GuidSourceId = Guid.NewGuid();
        return SourceTId == TId.LongSourceId ? LongSourceId.ToString() : GuidSourceId.ToString();
    }
    
    protected async Task<IEnumerable<EventEnvelop>> LoadEvents(Func<DbCommand> command)
    {
        await using DbDataReader reader = await command().ExecuteReaderAsync();

        List<EventEnvelop> events = [];
        while(await reader.ReadAsync())
        {
            LongSourceId = reader.GetInt64(EventSourceSchema.LongSourceId);
            GuidSourceId = reader.GetGuid(EventSourceSchema.GuidSourceId);

            var typeName = reader.GetString(EventSourceSchema.Type);
            var type = ResolveEventType(typeName);
            var json = reader.GetString(EventSourceSchema.Data);
            var sourcedEvent = JsonSerializer.Deserialize(json, type) as SourcedEvent?? default!;
            events.Add(new EventEnvelop(LongSourceId, GuidSourceId, sourcedEvent));
        }
        return events;
    }
    protected void PrepareSourceCommand(
        Func<Dictionary<string, object>, object[], int, DbParameter[]> parameters,
        DbCommand command, SourcedEvent[] events)
    {
        var sqlCommand = $"{InsertSourceCommand}";
        foreach (var (e, index) in events.Select((x, i) => (x, i)))
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
    protected async Task PrepareProjectionCommand(
    Func<IProjection, bool> subscribes, Func<string[], object[], DbParameter[]> getparams,
    DbCommand command, EventSourceEnvelop source, IEnumerable<IProjection> projections)
    {
        foreach (var projection in projections)
        {
            if(!subscribes(projection))
                continue;
            var type = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
            var record = ProjectionRestorer.Project(projection, source.SourcedEvents, type);
            string[] names = ["@longSourceId", "@guidSourceId", "@data", "@type", "@updatedAt"];
            var data = JsonSerializer.Serialize(record, type, SerializerOptions);
            object[] values = [source.LongSourceId, source.GuidSourceId, data, type.Name, DateTime.UtcNow];
            command.Parameters.Clear();
            command.Parameters.AddRange(getparams(names, values));
            command.CommandText = ApplyProjectionCommand(type.Name);
            await command.ExecuteNonQueryAsync();
        }
    }
}

public enum TId
{
    LongSourceId,
    GuidSourceId,
}