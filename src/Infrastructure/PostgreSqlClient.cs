using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Extensions;
using EventStorage.Projections;
using EventStorage.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis;

namespace EventStorage.Infrastructure;

public class PostgreSqlClient<T>(IServiceProvider sp, string conn) : ClientBase<T>(sp) where T : IEventSource
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger logger = TLogger.Create<PostgreSqlClient<T>>();
    public override async Task InitSource()
    {
        try
        {
            logger.LogInformation($"Begin initializing {nameof(PostgreSqlClient<T>)}.");
            _semaphore.Wait();
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            await using NpgsqlCommand sqlCommand = new(Schema.CreateSchemaIfNotExists, sqlConnection);
            sqlCommand.Transaction = sqlTransaction;
            await sqlCommand.ExecuteNonQueryAsync();
            foreach (var item in TProjections(x => true))
            {
                sqlCommand.CommandText = Schema.CreateProjectionIfNotExists(item?.Name?? "");
                await sqlCommand.ExecuteNonQueryAsync();
            }
            sqlCommand.CommandText = Schema.CreateCheckpointIfNotExists;
            await sqlCommand.ExecuteNonQueryAsync();
            sqlCommand.CommandText = Schema.CreateConcurrencyCheckFunction;
            await sqlCommand.ExecuteNonQueryAsync();
            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Finished initializing {nameof(PostgreSqlClient<T>)}.");
            _semaphore.Release();
        }
        catch (NpgsqlException e)
        {
            logger.LogInformation($"Failed initializing {nameof(PostgreSqlClient<T>)}. {e.Message}");
            throw;
        }
    }
    public override async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();
            
            IEnumerable<EventEnvelop> events = [];
            if(sourceId != null)
            {
                sqlCommand.CommandText = Schema.LoadEventSourceCommand(SourceTId.ToString());
                object param = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
                sqlCommand.Parameters.Add(new NpgsqlParameter("sourceId", param));
                events = await LoadEvents(() => sqlCommand);
                if(!events.Any())
                    throw new Exception("No such event source with this id exists.");
            }

            sourceId ??= await GenerateSourceId(sqlCommand);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);
            aggregate.RestoreAggregate(true, events.Select(x => x.SourcedEvent).ToArray());
            logger.LogInformation($"Finished restoring aggregate {typeof(T).Name}.");

            return aggregate;
        }
        catch(NpgsqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failed restoring aggregate {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failed restoring aggregate {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failed restoring aggregate {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public override async Task Commit(T aggregate)
    {
        var x = aggregate.PendingEvents.Count();
        logger.LogInformation($"Preparing to commit {x} pending event(s) for {typeof(T).Name} for {LongSourceId}");
        
        await using NpgsqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using NpgsqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
        await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.Transaction = sqlTransaction;
        try
        {
            // check for concurrent stream access
            await CheckConcurrency(sqlCommand, new NpgsqlParameter[]
            {
                new("sourceId" , LongSourceId),
                new ("expected", (object?)aggregate.EventStream.LastOrDefault()?.Version?? DBNull.Value)
            });
            
            // append events to event source
            if(aggregate.PendingEvents.Any())
            {
                await PrepareSourceCommand((names, values) => names.Select((x, i) => new NpgsqlParameter
                {
                    ParameterName = x.Key,
                    NpgsqlDbType = (NpgsqlDbType) x.Value,
                    NpgsqlValue = values[i]
                }).ToArray(), sqlCommand, aggregate.PendingEvents.ToArray());
            }
            
            // apply consistent projections if any
            var pending = aggregate.FlushPendingEvents();
            await PrepareProjectionCommand(p =>
                // does projection subscribes or reprojection wanted
                !ProjectionRestorer.Subscribes(pending, p) && pending.Any(),
                (names, values) => names.Select((x, i) => new NpgsqlParameter
                {
                    ParameterName = x.Key,
                    NpgsqlDbType = (NpgsqlDbType)x.Value,
                    NpgsqlValue = values[i]
                }).ToArray(),
                sqlCommand, new(LongSourceId, GuidSourceId, aggregate.EventStream),
                Projections.Where(x => x.Mode == ProjectionMode.Consistent)
            );
            
            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Committed {x} pending event(s) for {typeof(T).Name}");
            ProjectionPool.Release((ct) => new(LongSourceId, GuidSourceId, pending));
        }
        catch(NpgsqlException e)
        {
            await sqlTransaction.RollbackAsync();
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            await sqlTransaction.RollbackAsync();
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            await sqlTransaction.RollbackAsync();
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
    }
    public override async Task RestoreProjection(Projection p, IServiceProvider sp, params EventSourceEnvelop[] sources)
    {
        try
        {
            logger.LogInformation($"Restoring {p} with {sources.Length} event sources.");
            if(p.Configuration.Store == ProjectionStore.Redis)
            {
                var redis = sp.GetRequiredService<IRedisService>();
                await redis.RestoreProjection(p, sources);
            }

            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.Transaction = sqlTransaction;

            if(p.Configuration.Store == ProjectionStore.Selected)
            {
                sources.ToList().ForEach(async (source) =>
                    await PrepareProjectionCommand((p) => false,
                    (names, values) => names.Select((x, i) => new NpgsqlParameter
                    {
                        ParameterName = x.Key,
                        NpgsqlDbType = (NpgsqlDbType)x.Value,
                        NpgsqlValue = values[i]
                    }).ToArray(),
                    sqlCommand,
                    source,
                    [p], sp.GetRequiredService<IProjectionRestorer>())
                );
            }

            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Done restoring {p} with {sources.Length} event sources.");
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Restoring {p} failure. {e.Message}");
            throw;
        }
    }
    public override async Task<M?> Project<M>(string sourceId) where M : class
    {
        try
        {
            logger.LogInformation($"Starting {typeof(M).Name} projection.");
            var projection = ServiceProvider.GetService<IProjection<M>>();
            if(projection == null)
                return default;

            if(projection.Configuration.Store == ProjectionStore.Redis)
                return await Redis.GetDocument<M>(sourceId);
            
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();

            object id = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
            if(projection.Mode != ProjectionMode.Transient)
            {
                sqlCommand.CommandText = Schema.GetDocumentCommand<M>(SourceTId.ToString());
                sqlCommand.Parameters.AddWithValue("@sourceId", id);
                await using NpgsqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if(!await reader.ReadAsync())
                    return default;
                var json = reader.GetString(EventStorageSchema.Data);
                var m = JsonSerializer.Deserialize<M>(json);
                logger.LogInformation($"{typeof(M).Name} projection completed.");
                return m;
            }

            sqlCommand.CommandText = Schema.LoadEventSourceCommand(SourceTId.ToString());
            sqlCommand.Parameters.Add(new NpgsqlParameter("sourceId", id));
            var events = await LoadEvents(() => sqlCommand);
            var model = ProjectionRestorer.Project<M>(events.Select(x => x.SourcedEvent));
            logger.LogInformation($"{typeof(M).Name} projection completed.");
            return model;
        }
        catch(NpgsqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {typeof(M).Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {typeof(M).Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {typeof(M).Name}. {e.Message}");
            throw;
        }
    }
    public override async Task<Checkpoint> LoadCheckpoint(IProjection projection)
    {
        try
        {
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = new(Schema.LoadCheckpointCommand, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@subscription", nameof(projection));
            sqlCommand.Parameters.AddWithValue("@type", (int)CheckpointType.Projection);
            NpgsqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
            Checkpoint checkpoint = new(nameof(projection), 0, 0, CheckpointType.Projection);
            long seq = 0;
            if(await reader.ReadAsync())
                seq = reader.GetInt64("sequence");
            else
                await SaveCheckpoint(checkpoint, true);

            await reader.DisposeAsync();
            sqlCommand.CommandText = Schema.GetMaxSequenceId;
            reader = await sqlCommand.ExecuteReaderAsync();
            await reader.ReadAsync();
            long maxSeq = reader.HasRows ? (long)reader.GetValue(0) : 0;
            await reader.DisposeAsync();
            return checkpoint with { Seq = seq, MaxSeq = maxSeq};
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Checkpoint load failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public override async Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false)
    {
        try
        {
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = insert ? Schema.InsertCheckpointCommand : Schema.SaveCheckpointCommand;
            sqlCommand.Parameters.Add(new NpgsqlParameter("subscription", checkpoint.Subscription));
            sqlCommand.Parameters.Add(new NpgsqlParameter("sequence", checkpoint.Seq));
            sqlCommand.Parameters.Add(new NpgsqlParameter("type", (int)checkpoint.Type));
            await sqlCommand.ExecuteNonQueryAsync();
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Save checkpoint failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public override async Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c)
    {
        try
        {
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = new(Schema.LoadEventsPastCheckpoint, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@seq", c.Seq);
            sqlCommand.Parameters.AddWithValue("@maxSeq", c.MaxSeq);
            var events = await LoadEvents(() => sqlCommand);
            return events;
        }
        catch(NpgsqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Loading events failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Loading events failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Loading events failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public override async Task<IEnumerable<EventEnvelop>> LoadEventSource(long sourceId)
    {
        try
        {
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = Schema.LoadEventSourceCommand(TId.LongSourceId.ToString());
            sqlCommand.Parameters.AddWithValue("@sourceId", sourceId);
            var events = await LoadEvents(() => sqlCommand);
            return events;
        }
        catch(NpgsqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Loading events failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Loading events failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Loading events failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
}