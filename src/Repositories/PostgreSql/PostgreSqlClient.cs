using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Events;
using EventStorage.Extensions;
using EventStorage.Projections;
using EventStorage.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace EventStorage.Repositories.PostgreSql;

public class PostgreSqlClient<T>(string conn, IServiceProvider sp)
    : ClientBase<T>(sp, EventStore.PostgresSql), IPostgreSqlClient<T> where T : IEventSource
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger logger = sp.GetRequiredService<ILogger<PostgreSqlClient<T>>>();
    public async Task Init()
    {
        try
        {
            _semaphore.Wait();
            logger.LogInformation($"Begin initializing {nameof(PostgreSqlClient<T>)}.");
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
            await using NpgsqlCommand command = sqlConnection.CreateCommand();

            sourceId ??= await GenerateSourceId(command);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);
            
            object param = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
            var sourcedEvents = await LoadEventSource(command, () => new NpgsqlParameter("sourceId", param));
            aggregate.RestoreAggregate(RestoreType.Stream, sourcedEvents.ToArray());
            logger.LogInformation($"Finished restoring aggregate {typeof(T).Name}.");

            return aggregate;
        }
        catch(PostgresException e)
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
                if(pending.Any() && !Projection.Subscribes(pending, projection))
                    continue;
                var model = projection.GetType().BaseType?.GenericTypeArguments.First()?? default!;
                var record = Projection.Project(projection, aggregate.EventStream, model);
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
        catch(PostgresException e)
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
        await using NpgsqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using NpgsqlCommand command = sqlConnection.CreateCommand();

        object id = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
        var events = await LoadEventSource(command, () => new NpgsqlParameter("sourceId", id));
        if(!events.Any())
            return default;
        var model = Projection.Project<M>(events);
        return model;
    }
}