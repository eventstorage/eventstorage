using System.Data;
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
using StackExchange.Redis;

namespace EventStorage.Infrastructure;

public class SqlServerClient<T>(IServiceProvider sp, string conn) : ClientBase<T>(sp) where T : IEventSource
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger logger = TLogger.Create<SqlServerClient<T>>();
    public override async Task InitSource()
    {
        try
        {
            logger.LogInformation($"Begin initializing {nameof(SqlServerClient<T>)}.");
            _semaphore.Wait();
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            await using SqlCommand command = new(Schema.CreateSchemaIfNotExists, sqlConnection);
            command.Transaction = sqlTransaction;
            await command.ExecuteNonQueryAsync();
            foreach (var item in TProjections(t => true))
            {
                command.CommandText = Schema.CreateProjectionIfNotExists(item?.Name?? "");
                await command.ExecuteNonQueryAsync();
            }
            command.CommandText = Schema.CreateCheckpointIfNotExists;
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
    public override async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = sqlConnection.CreateCommand();

            IEnumerable<EventEnvelop> events = [];
            if(sourceId != null)
            {
                sqlCommand.CommandText = Schema.LoadEventSourceCommand(SourceTId.ToString());
                sqlCommand.Parameters.Add(new SqlParameter("sourceId", sourceId));
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
    public override async Task Commit(T aggregate)
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
            // check for concurrent stream access
            await CheckConcurrency(sqlCommand, new SqlParameter[]
            {
                new("sourceId" , LongSourceId),
                new ("expected", (object?)aggregate.EventStream.LastOrDefault()?.Version?? DBNull.Value)
            });

            // add event source to event storage
            if(aggregate.PendingEvents.Any())
            {
                await PrepareSourceCommand((names, values) => names.Select((x, i) => new SqlParameter
                {
                    ParameterName = x.Key,
                    SqlDbType = (SqlDbType)x.Value,
                    SqlValue = values[i]
                }).ToArray(), sqlCommand, aggregate.PendingEvents.ToArray());
            }

            // apply consistent projections if any
            var pending = aggregate.FlushPendingEvents();
            await PrepareProjectionCommand(p =>
                // does projection subscribes or reprojection wanted
                !ProjectionRestorer.Subscribes(pending, p) && pending.Any(),
                (names, values) => names.Select((x, i) => new SqlParameter
                {
                    ParameterName = x.Key,
                    SqlDbType = (SqlDbType)x.Value,
                    SqlValue = values[i]
                }).ToArray(),
                sqlCommand, new(LongSourceId, GuidSourceId, aggregate.EventStream),
                Projections.Where(x => x.Mode == ProjectionMode.Consistent)
            );

            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Committed {x} pending event(s) for {typeof(T).Name}");

            EventSourceEnvelop envelop = new(LongSourceId, GuidSourceId, aggregate.EventStream);
            if(Projections.Any(x => x.Mode == ProjectionMode.Async))
                ProjectionPool.Release((scope, ct) => RestoreProjections(envelop, scope));
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
            await sqlTransaction.RollbackAsync();
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            await sqlTransaction.RollbackAsync();
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public override async Task<long> RestoreProjections(EventSourceEnvelop source, IServiceScopeFactory scope)
    {
        try
        {
            logger.LogInformation($"Restoring projections for event source {source.LId}.");
            var sp = scope.CreateScope().ServiceProvider;
            var projections = sp.GetServices<IProjection>().Where(x => x.Mode != ProjectionMode.Transient);
            var restorer = sp.GetRequiredService<IProjectionRestorer>();
            if(projections.Any(x => x.Configuration.Store == ProjectionStore.Redis))
            {
                var redis = sp.GetRequiredService<IRedisService>();
                var ps = projections.Where(x => x.Configuration.Store == ProjectionStore.Redis);
                await redis.RestoreProjections(source, ps, restorer);
            }
            if(projections.Any(x => x.Configuration.Store == ProjectionStore.Selected))
            {
                await using SqlConnection sqlConnection = new(conn);
                await sqlConnection.OpenAsync();
                await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
                await using SqlCommand sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.Transaction = sqlTransaction;

                await PrepareProjectionCommand((p) => !restorer.Subscribes(source.SourcedEvents, p),
                    (names, values) => names.Select((x, i) => new SqlParameter
                    {
                        ParameterName = x.Key,
                        SqlDbType = (SqlDbType)x.Value,
                        SqlValue = values[i]
                    }).ToArray(),
                    sqlCommand, source,
                    projections.Where(x => x.Configuration.Store == ProjectionStore.Selected), restorer
                );

                await sqlTransaction.CommitAsync();
                logger.LogInformation($"Restored projections for event source {source.LId}.");
            }
            return source.LId;
        }
        catch(RedisException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure restoring projections for source {source.LId}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure restoring projections for source {source.LId}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure restoring projections for source {source.LId}. {e.Message}");
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

            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand command = sqlConnection.CreateCommand();

            if(projection.Mode != ProjectionMode.Transient)
            {
                command.CommandText = Schema.GetDocumentCommand<M>(SourceTId.ToString());
                command.Parameters.AddWithValue("@sourceId", sourceId);
                await using SqlDataReader reader = await command.ExecuteReaderAsync();
                if(!await reader.ReadAsync())
                    return default;
                var json = reader.GetString(EventStorageSchema.Data);
                var m = JsonSerializer.Deserialize<M>(json);
                logger.LogInformation($"{typeof(M).Name} projection completed.");
                return m;
            }

            command.CommandText = Schema.LoadEventSourceCommand(SourceTId.ToString());
            command.Parameters.Add(new SqlParameter("sourceId", sourceId));
            var events = await LoadEvents(() => command);
            var model = ProjectionRestorer.Project<M>(events.Select(x => x.SourcedEvent));
            logger.LogInformation($"{typeof(M).Name} projection completed.");
            return model;
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
    public override async Task<Checkpoint> LoadCheckpoint()
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = new (Schema.LoadCheckpointCommand, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@type", CheckpointType.Projection);
            SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
            Checkpoint checkpoint = new(0, 0, CheckpointType.Projection);
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
            return checkpoint with { MaxSeq = maxSeq, Seq = seq};
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
    public override async Task SaveCheckpoint(Checkpoint checkpoint, bool insert = false)
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = insert ? Schema.InsertCheckpointCommand : Schema.SaveCheckpointCommand;
            sqlCommand.Parameters.AddWithValue("@sequence", checkpoint.Seq);
            sqlCommand.Parameters.AddWithValue("@type", checkpoint.Type);
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
    public override async Task<IEnumerable<EventEnvelop>> LoadEventsPastCheckpoint(Checkpoint c)
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = new(Schema.LoadEventsPastCheckpoint, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@seq", c.Seq);
            sqlCommand.Parameters.AddWithValue("@maxSeq", c.MaxSeq);
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
    public override async Task<IEnumerable<EventEnvelop>> LoadEventSource(long sourceId)
    {
        try
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = Schema.LoadEventSourceCommand(SourceTId.ToString());
            sqlCommand.Parameters.AddWithValue("@sourceId", sourceId);
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