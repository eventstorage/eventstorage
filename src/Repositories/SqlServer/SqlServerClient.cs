using System.Data;
using System.Data.Common;
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
using StackExchange.Redis;

namespace EventStorage.Repositories.SqlServer;

public class SqlServerClient<T>(string conn, IServiceProvider sp, EventStore source)
    : ClientBase<T>(sp, source), ISqlServerClient<T> where T : IEventSource
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger logger = sp.GetRequiredService<ILogger<SqlServerClient<T>>>();
    public async Task Init()
    {
        logger.LogInformation($"Begin initializing {nameof(SqlServerClient<T>)}.");
        _semaphore.Wait();
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            await using SqlCommand command = new(CreateSchemaIfNotExists, sqlConnection);
            command.Transaction = sqlTransaction;
            await command.ExecuteNonQueryAsync();
            foreach (var item in TProjections(t => true))
            {
                command.CommandText = CreateProjectionIfNotExists(item?.Name?? "");
                await command.ExecuteNonQueryAsync();
            }
            command.CommandText = CreateCheckpointIfNotExists;
            await command.ExecuteNonQueryAsync();
            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Finished initializing {nameof(SqlServerClient<T>)}.");
            _semaphore.Release();
        }
        catch(SqlException e)
        {
            logger.LogInformation($"Failed initializing {nameof(SqlServerClient<T>)}. {e.Message}");
            throw;
        }
    }
    public async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");

            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = sqlConnection.CreateCommand();

            sourceId ??= await GenerateSourceId(sqlCommand);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);

            sqlCommand.CommandText = GetSourceCommand;
            sqlCommand.Parameters.Add(new SqlParameter("sourceId", sourceId));
            var events = await LoadEvents(() => sqlCommand);
            aggregate.RestoreAggregate(RestoreType.Stream, events.Select(x => x.SourcedEvent).ToArray());
            logger.LogInformation($"Finished restoring aggregate {typeof(T).Name}.");

            return aggregate;
        }
        catch(SqlException e)
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
        
        await using SqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
        await using SqlCommand sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.Transaction = sqlTransaction;
        try
        {
            // add event source to event storage
            if(aggregate.PendingEvents.Any())
            {
                PrepareSourceCommand((names, values, count) => values.Select((x, i) => new SqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    SqlDbType = (SqlDbType)names.Values.ElementAt(i),
                    SqlValue = x
                }).ToArray(), sqlCommand, aggregate.PendingEvents.ToArray());
                await sqlCommand.ExecuteNonQueryAsync();
            }

            // apply consistent projections if any
            var pending = aggregate.CommitPendingEvents();
            SqlDbType[] types = [SqlDbType.BigInt, SqlDbType.UniqueIdentifier, SqlDbType.NVarChar,
            SqlDbType.NVarChar, SqlDbType.DateTime];
            await PrepareProjectionCommand(projection =>
                // does projection subscribes or reprojection wanted
                !ProjectionRestorer.Subscribes(pending, projection) && pending.Any(),
                (names, values) => names.Select((x, i) => new SqlParameter {
                        ParameterName = names[i],
                        SqlDbType = types[i],
                        SqlValue = values[i]
                    }).ToArray(),
                sqlCommand, new(LongSourceId, GuidSourceId, aggregate.EventStream),
                Projections.Where(x => x.Mode == ProjectionMode.Consistent), ProjectionRestorer
            );

            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Committed {x} pending event(s) for {typeof(T).Name}");
            var envelop = new EventSourceEnvelop(LongSourceId, GuidSourceId, aggregate.EventStream);
            ProjectionPoll.Release((scope, ct) => RestoreProjections(envelop, scope));
        }
        catch(SqlException e)
        {
            await sqlTransaction.RollbackAsync();
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public async Task RestoreProjections(EventSourceEnvelop source, IServiceScopeFactory scope)
    {
        try
        {
            logger.LogInformation($"Restoring projections for event source {source.LongSourceId}.");
            var sp = scope.CreateScope().ServiceProvider;
            var projections = sp.GetServices<IProjection>();
            if(projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
                await Redis.RestoreProjections(source);
            if(projections.Any(x => x.Configuration.Store == ProjectionStore.Selected))
            {
                await using SqlConnection sqlConnection = new(conn);
                await sqlConnection.OpenAsync();
                await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
                await using SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.Transaction = sqlTransaction;

                SqlDbType[] types = [SqlDbType.BigInt, SqlDbType.UniqueIdentifier, SqlDbType.NVarChar,
                SqlDbType.NVarChar, SqlDbType.DateTime];
                var restorer = sp.GetRequiredService<IProjectionRestorer>();
                await PrepareProjectionCommand((p) => !restorer.Subscribes(source.SourcedEvents, p),
                    (names, values) => names.Select((x, i) => new SqlParameter
                        {
                            ParameterName = names[i],
                            SqlDbType = types[i],
                            SqlValue = values[i]
                        }).ToArray(),
                    sqlCommand, source,
                    projections.Where(x => x.Configuration.Store == ProjectionStore.Selected), restorer
                );

                await sqlTransaction.CommitAsync();
                logger.LogInformation($"Restored projections for event source {source.LongSourceId}.");
            }
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure restoring projections for source {source.LongSourceId}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure restoring projections for source {source.LongSourceId}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure restoring projections for source {source.LongSourceId}. {e.Message}");
            // throw;
        }
    }
    public async Task<M?> Project<M>(string sourceId) where M : class
    {
        try
        {
            logger.LogInformation($"Starting {typeof(M).Name} projection.");
            var projection = Sp.GetService<IProjection<M>>();
            if(projection == null)
                return default;
                
            if(projection.Configuration.Store != ProjectionStore.Selected)
                return await Redis.GetDocument<M>(sourceId);

            logger.LogInformation($"Starting {typeof(M).Name} projection.");
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand command = sqlConnection.CreateCommand();

            if(projection.Mode != ProjectionMode.Runtime)
            {
                command.CommandText = GetDocumentCommand<M>();
                command.Parameters.AddWithValue("@sourceId", sourceId);
                await using SqlDataReader reader = await command.ExecuteReaderAsync();
                if(!await reader.ReadAsync())
                    return default;
                var json = reader.GetString(EventSourceSchema.Data);
                var m = JsonSerializer.Deserialize<M>(json);
                logger.LogInformation($"{typeof(M).Name} projection completed.");
                return m;
            }

            command.CommandText = GetSourceCommand;
            command.Parameters.Add(new SqlParameter("sourceId", sourceId));
            var events = await LoadEvents(() => command);
            var model = ProjectionRestorer.Project<M>(events.Select(x => x.SourcedEvent));
            logger.LogInformation($"{typeof(M).Name} projection completed.");
            return model;
        }
        catch(RedisException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {typeof(M).Name}. {e.Message}");
            throw;
        }
        catch(SqlException e)
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
            sqlCommand.Parameters.AddWithValue("@type", CheckpointType.Projection.ToString());
            sqlCommand.Parameters.AddWithValue("@sourceType", typeof(T).Name);
            await using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
            Checkpoint checkpoint = new(0, CheckpointType.Projection, typeof(T).Name);
            if(!await reader.ReadAsync())
            {
                await SaveCheckpoint(checkpoint, true);
                return checkpoint;
            }
            var seq = reader.GetInt64("sequence");
            var type = Enum.Parse<CheckpointType>(reader.GetString("type"));
            var sourceType = reader.GetString("sourceType");
            return checkpoint with { Sequence = seq, Type = type, SourceType = sourceType };
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Checkpoint load failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Checkpoint load failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public async Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false)
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = insert ? InsertCheckpointCommand : SaveCheckpointCommand;
            sqlCommand.Parameters.AddWithValue("@sequence", checkpoint.Sequence);
            sqlCommand.Parameters.AddWithValue("@type", checkpoint.Type);
            sqlCommand.Parameters.AddWithValue("@sourceType", checkpoint.SourceType);
            await sqlCommand.ExecuteNonQueryAsync();
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Save checkpoint failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Save checkpoint failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public async Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c)
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = new(LoadEventsPastCheckpointCommand, sqlConnection);
            sqlCommand.Parameters.Add(new SqlParameter("sequence", c.Sequence));
            var events = await LoadEvents(() => sqlCommand);
            return events;
        }
        catch(SqlException e)
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