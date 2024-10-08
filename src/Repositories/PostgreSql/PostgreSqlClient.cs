using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Events;
using EventStorage.Extensions;
using EventStorage.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace EventStorage.Repositories.PostgreSql;

public class PostgreSqlClient<T>(string conn, IServiceProvider sp)
    : ClientBase<T>(sp, EventSources.PostgresSql), IPostgreSqlClient<T> where T : IAggregateRoot
{
    private readonly ILogger logger = sp.GetRequiredService<ILogger<PostgreSqlClient<T>>>();
    public async Task Init()
    {
        logger.LogInformation($"Begin initializing {nameof(PostgreSqlClient<T>)}.");
        await using NpgsqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using NpgsqlCommand sqlCommand = new(CreateSchemaIfNotExists, sqlConnection);
        await sqlCommand.ExecuteNonQueryAsync();
        logger.LogInformation($"Finished initializing {nameof(PostgreSqlClient<T>)}.");
    }
    public async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");

            await using NpgsqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using NpgsqlCommand command = new("", sqlConnection);

            sourceId ??= await GenerateSourceId(command);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);
            
            object param = SourceTId == TId.LongSourceId ? long.Parse(sourceId) : Guid.Parse(sourceId);
            command.CommandText = GetSourceCommand;
            command.Parameters.AddWithValue("@sourceId", param);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            
            List<SourcedEvent> sourcedEvents = [];
            while(await reader.ReadAsync())
            {
                LongSourceId = reader.GetInt64(EventSourceSchema.LongSourceId);
                GuidSourceId = reader.GetGuid(EventSourceSchema.GuidSourceId);

                var typeName = reader.GetString(EventSourceSchema.Type);
                var type = ResolveEventType(typeName);
                var jsonData = reader.GetString(EventSourceSchema.Data);
                var sourcedEvent = JsonSerializer.Deserialize(jsonData, type) as SourcedEvent ??
                    throw new SerializationException($"Deserialize failure for event type {type}, sourceId {sourceId}.");
                sourcedEvents.Add(sourcedEvent);
            };
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
}