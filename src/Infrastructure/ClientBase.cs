using System.Data;
using System.Data.Common;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Projections;
using EventStorage.Schema;
using EventStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using TDiscover;

namespace EventStorage.Infrastructure;

public abstract class ClientBase<T>(IServiceProvider sp) : IEventStorage<T> where T : IEventSource
{
    public IServiceProvider ServiceProvider => sp;
    protected IEventStorageSchema Schema = sp.GetRequiredService<IEventStorageSchema>();
    protected readonly static JsonSerializerOptions SerializerOptions = new() { IncludeFields = true };
    protected long LongSourceId { get; set; } = 0;
    protected Guid GuidSourceId { get; set; }
    private readonly Type? _genericTypeArg = typeof(T).BaseType?.GenericTypeArguments[0];
    protected TId SourceTId => _genericTypeArg != null &&
        _genericTypeArg.IsAssignableFrom(typeof(long)) ? TId.LongSourceId : TId.GuidSourceId;
    protected IRedisService Redis => ServiceProvider.GetRequiredService<IRedisService>();
    protected IProjectionRestorer ProjectionRestorer => sp.GetRequiredService<IProjectionRestorer>();
    protected IAsyncProjectionPool ProjectionPool => sp.GetRequiredService<IAsyncProjectionPool>();
    protected IEnumerable<IProjection> Projections => ServiceProvider.GetServices<IProjection>();
    public abstract Task InitSource();
    public abstract Task<T> CreateOrRestore(string? sourceId = null);
    public abstract Task Commit(T t);
    public abstract Task<M?> Project<M>(string sourceId) where M : class;
    public abstract Task<IEnumerable<EventEnvelop>> LoadEventSource(long sourceId);
    public abstract Task<long> LoadMaxSequence();
    public abstract Task<Checkpoint> LoadCheckpoint(IProjection projection);
    public abstract Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false);
    public abstract Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c);
    public abstract Task RestoreProjection(Projection projection, IServiceProvider sp, params EventSourceEnvelop[] sources);
    protected Type ResolveEventType(string typeName) => Td.FindByTypeName<SourcedEvent>(typeName)??
        throw new Exception($"Couldn't determine event type while resolving {typeName}.");
    #pragma warning disable CS8619
    protected IEnumerable<Type> TProjections(Func<IProjection, bool> predicate) =>
        Projections.Where(predicate)
        .Where(p => p.Mode != ProjectionMode.Transient)
        .Where(p => p.Configuration.Store == ProjectionStore.Selected)
        .Select(p => p.GetType().BaseType?.GenericTypeArguments.First());

    protected async Task<string> GenerateSourceId(DbCommand command)
    {
        command.CommandText = Schema.GetMaxSourceId;
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
            var sequence = reader.GetInt64("Sequence");
            LongSourceId = reader.GetInt64(EventStorageSchema.LongSourceId);
            GuidSourceId = reader.GetGuid(EventStorageSchema.GuidSourceId);

            var typeName = reader.GetString(EventStorageSchema.Type);
            var type = ResolveEventType(typeName);
            var json = reader.GetString(EventStorageSchema.Data);
            var sourcedEvent = JsonSerializer.Deserialize(json, type) as SourcedEvent?? default!;
            events.Add(new(sequence, LongSourceId, GuidSourceId, sourcedEvent));
        }
        return events;
    }
    protected async Task PrepareSourceCommand(
        Func<Dictionary<string, object>, object[], DbParameter[]> parameters,
        DbCommand command, SourcedEvent[] events)
    {
        foreach (var e in events)
        {
            object[] values = [e.Id, LongSourceId, GuidSourceId, e.Version,
            e.GetType().Name, JsonSerializer.Serialize(e, e.GetType(), SerializerOptions),
            DateTime.UtcNow, typeof(T).Name, e.TenantId?? "default", e.CorrelationId??
            "default", e.CausationId?? "default"];
            command.Parameters.Clear();
            command.CommandText = Schema.AddEventsCommand;
            command.Parameters.AddRange(parameters(Schema.EventStorageFields, values));
            await command.ExecuteNonQueryAsync();
        }
    }
    protected async Task PrepareProjectionCommand(
        Func<IProjection, bool> subscribes,
        Func<Dictionary<string, object>, object[], DbParameter[]> parameters,
        DbCommand command, EventSourceEnvelop source, IEnumerable<IProjection> projections,
        IProjectionRestorer? restorer = null)
    {
        foreach (var projection in projections)
        {
            if(!subscribes(projection))
                continue;
            restorer ??= ProjectionRestorer;
            var type = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
            var record = restorer.Project(projection, source.SourcedEvents, type);
            var data = JsonSerializer.Serialize(record, type, SerializerOptions);
            object[] values = [source.LId, source.GId, data, type.Name, DateTime.UtcNow];
            command.Parameters.Clear();
            command.Parameters.AddRange(parameters(Schema.ProjectionFields, values));
            command.CommandText = Schema.AddProjectionsCommand(type.Name);
            await command.ExecuteNonQueryAsync();
        }
    }
    protected async Task CheckConcurrency(DbCommand command, DbParameter[] parameters)
    {
        command.CommandText = Schema.CheckConcurrency;
        command.Parameters.Clear();
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }
}

public enum TId
{
    LongSourceId,
    GuidSourceId,
}