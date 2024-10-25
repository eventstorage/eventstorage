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
        var events = aggregate.PendingEvents.Count();
        logger.LogInformation($"Preparing to commit {events} pending event(s) for {aggregate.GetType().Name}");
        try
        {
            if(aggregate.PendingEvents.Any())
            {
                await using NpgsqlConnection sqlConnection = new(conn);
                await sqlConnection.OpenAsync();
                await using NpgsqlCommand command = sqlConnection.CreateCommand();
                PrepareCommand((names, values, count) => values.Select((x, i) => new NpgsqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    NpgsqlDbType = (NpgsqlDbType)names.Values.ElementAt(i),
                    NpgsqlValue = x
                }).ToArray(), command, aggregate.PendingEvents.ToArray());
                await command.ExecuteNonQueryAsync();
            }
            aggregate.CommitPendingEvents();
            logger.LogInformation($"Committed {events} pending event(s) for {aggregate.GetType().Name}");
        }
        catch(PostgresException e)
        {
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

        var projection = Sp.GetRequiredService<IProjectionEngine>();
        object id = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
        var events = await LoadEventSource(command, () => new NpgsqlParameter("sourceId", id));
        var model = projection.Project<M>(events);
        return model;
    }
}