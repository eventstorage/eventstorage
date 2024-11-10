using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
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

namespace EventStorage.Repositories.PostgreSql;

public class PostgreSqlClient<T>(string conn, IServiceProvider sp)
    : ClientBase<T>(sp, EventStore.PostgresSql), IPostgreSqlClient<T> where T : IEventSource
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger logger = sp.GetRequiredService<ILogger<PostgreSqlClient<T>>>();
    public async Task Init()
    {
        logger.LogInformation($"Begin initializing {nameof(PostgreSqlClient<T>)}.");
        _semaphore.Wait();
        try
        {
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            await using NpgsqlCommand sqlCommand = new(CreateSchemaIfNotExists, sqlConnection);
            sqlCommand.Transaction = sqlTransaction;
            await sqlCommand.ExecuteNonQueryAsync();
            foreach (var item in TProjections(x => true))
            {
                sqlCommand.CommandText = CreateProjectionIfNotExists(item?.Name?? "");
                await sqlCommand.ExecuteNonQueryAsync();
            }
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
    public async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");

            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();

            sourceId ??= await GenerateSourceId(sqlCommand);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);
            
            sqlCommand.CommandText = GetSourceCommand;
            object param = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
            sqlCommand.Parameters.Add(new NpgsqlParameter("sourceId", param));
            var events = await LoadEvents(() => sqlCommand);
            aggregate.RestoreAggregate(RestoreType.Stream, events.ToArray());
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
    public async Task Commit(T aggregate)
    {
        var x = aggregate.PendingEvents.Count();
        logger.LogInformation($"Preparing to commit {x} pending event(s) for {typeof(T).Name}");
        
        await using NpgsqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using NpgsqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
        await using NpgsqlCommand command = sqlConnection.CreateCommand();
        command.Transaction = sqlTransaction;
        try
        {
            // add event source to event storage
            if(aggregate.PendingEvents.Any())
            {
                PrepareCommand((names, values, count) => values.Select((x, i) => new NpgsqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    NpgsqlDbType = (NpgsqlDbType)names.Values.ElementAt(i),
                    NpgsqlValue = x
                }).ToArray(), command, aggregate.PendingEvents.ToArray());
                await command.ExecuteNonQueryAsync();
            }
            
            // apply consistent projections if any
            var pending = aggregate.CommitPendingEvents();
            foreach (var projection in Projections.Where(x => x.Mode == ProjectionMode.Consistent))
            {
                if(pending.Any() && !ProjectionRestorer.Subscribes(pending, projection))
                    continue;
                var model = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
                var record = ProjectionRestorer.Project(projection, aggregate.EventStream, model);
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@longSourceId", LongSourceId);
                command.Parameters.AddWithValue("@guidSourceId", GuidSourceId);
                var data = JsonSerializer.Serialize(record, model, SerializerOptions);
                command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, data);
                command.Parameters.AddWithValue("@type", model?.Name?? "");
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                command.CommandText = ApplyProjectionCommand(model?.Name?? "");
                await command.ExecuteNonQueryAsync();
            }

            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Committed {x} pending event(s) for {typeof(T).Name}");
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
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
    }
    public async Task<M?> Project<M>(string sourceId)
    {
        try
        {
            logger.LogInformation($"Starting {typeof(M).Name} projection.");
            var projection = Sp.GetService<IProjection<M>>();
            if(projection == null)
                return default;

            if(projection.Configuration.Store != ProjectionStore.Selected)
                return await Redis.GetDocument<M>(sourceId);
            
            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand sqlCommand = sqlConnection.CreateCommand();

            object id = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
            if(projection.Mode != ProjectionMode.Runtime)
            {
                sqlCommand.CommandText = GetDocumentCommand<M>();
                sqlCommand.Parameters.AddWithValue("@sourceId", id);
                await using NpgsqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
                if(!await reader.ReadAsync())
                    return default;
                var json = reader.GetString(EventSourceSchema.Data);
                var m = JsonSerializer.Deserialize<M>(json);
                logger.LogInformation($"{typeof(M).Name} projection completed.");
                return m;
            }

            sqlCommand.CommandText = GetSourceCommand;
            sqlCommand.Parameters.Add(new NpgsqlParameter("sourceId", id));
            var events = await LoadEvents(() => sqlCommand);
            var model = ProjectionRestorer.Project<M>(events);
            logger.LogInformation($"{typeof(M).Name} projection completed.");
            return model;
        }
        catch(RedisException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {typeof(M).Name}. {e.Message}");
            throw;
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
    public async Task<Checkpoint> LoadCheckpoint()
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = new (LoadCheckpointCommand, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@type", CheckpointType.Projection);
            sqlCommand.Parameters.AddWithValue("@sourceType", typeof(T).Name);
            await using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
            await reader.ReadAsync();
            var sequence = reader.GetInt64("sequence");
            var type = reader.GetString("type");
            var sourceType = reader.GetString("sourceType");
            return new Checkpoint(sequence, Enum.Parse<CheckpointType>(type), sourceType);
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Checkpoint load failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public async Task SaveCheckpoint(Checkpoint checkpoint)
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = new (SaveCheckpointCommand, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@sequence", checkpoint.Sequence);
            sqlCommand.Parameters.AddWithValue("@type", checkpoint.Type);
            sqlCommand.Parameters.AddWithValue("@sourceType", checkpoint.SourceType);
            await sqlCommand.ExecuteNonQueryAsync();
        }
        catch(NpgsqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Saving checkpoint failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Saving checkpoint failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public async Task<IEnumerable<SourcedEvent>> LoadEventsPastCheckpoint(Checkpoint c)
    {
        try
        {
            await using NpgsqlConnection sqlConnection = new(conn);
            await using NpgsqlCommand sqlCommand = new(LoadEventsPastSeqCommand, sqlConnection);
            sqlCommand.Parameters.Add(new NpgsqlParameter("sequence", c.Sequence));
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