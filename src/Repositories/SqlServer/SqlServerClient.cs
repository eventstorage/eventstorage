using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Extensions;
using EventStorage.Projections;
using EventStorage.Repositories.Redis;
using EventStorage.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TDiscover;

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
            await using SqlCommand command = sqlConnection.CreateCommand();

            sourceId ??= await GenerateSourceId(command);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);

            var events = await LoadEventSource(command, () => new SqlParameter("sourceId", sourceId));
            aggregate.RestoreAggregate(RestoreType.Stream, events.ToArray());
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
        await using SqlCommand command = sqlConnection.CreateCommand();
        command.Transaction = sqlTransaction;
        try
        {
            // add event source to event storage
            if(aggregate.PendingEvents.Any())
            {
                PrepareCommand((names, values, count) => values.Select((x, i) => new SqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    SqlDbType = (SqlDbType)names.Values.ElementAt(i),
                    SqlValue = x
                }).ToArray(), command, aggregate.PendingEvents.ToArray());
                await command.ExecuteNonQueryAsync();
            }

            // apply consistent projections if any
            var pending = aggregate.CommitPendingEvents();
            foreach (var projection in Projections.Where(x => x.Mode == ProjectionMode.Consistent))
            {
                if(pending.Any() && !Projection.Subscribes(pending, projection))
                    continue;
                var model = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
                var record = Projection.Project(projection, aggregate.EventStream, model);
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@longSourceId", LongSourceId);
                command.Parameters.AddWithValue("@guidSourceId", GuidSourceId);
                var data = JsonSerializer.Serialize(record, model, SerializerOptions);
                command.Parameters.AddWithValue("@data", data);
                command.Parameters.AddWithValue("@type", model?.Name);
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                command.CommandText = ApplyProjectionCommand(model?.Name?? "");
                await command.ExecuteNonQueryAsync();
            }

            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Committed {x} pending event(s) for {typeof(T).Name}");
        }
        catch(SqlException e)
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

            var events = await LoadEventSource(command, () => new SqlParameter("sourceId", sourceId));
            var model = Projection.Project<M>(events);
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
}