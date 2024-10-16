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
        using SqlConnection sqlConnection = new(conn);
        sqlConnection.Open();
        using SqlCommand command = new(CreateSchemaIfNotExists, sqlConnection);
        await command.ExecuteNonQueryAsync();
        logger.LogInformation($"Finished initializing {nameof(SqlServerClient<T>)}.");
        _semaphore.Release();
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
            
            command.CommandText = GetSourceCommand;
            command.Parameters.AddWithValue("@sourceId", sourceId);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            
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
        var events = aggregate.PendingEvents.Count();
        logger.LogInformation($"Preparing to commit {events} pending event(s) for {aggregate.GetType().Name}");
        try
        {
            if(aggregate.PendingEvents.Any())
            {
                await using SqlConnection sqlConnection = new(conn);
                await sqlConnection.OpenAsync();
                await using SqlCommand command = sqlConnection.CreateCommand();
                PrepareCommand((names, values, count) => values.Select((x, i) => new SqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    SqlDbType = (SqlDbType)names.Values.ElementAt(i),
                    SqlValue = x
                }).ToArray(), command, aggregate.PendingEvents.ToArray());
                await command.ExecuteNonQueryAsync();
            }
            aggregate.CommitPendingEvents();
            logger.LogInformation($"Committed {events} pending event(s) for {aggregate.GetType().Name}");
        }
        catch(SqlException e)
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
    public async Task<M> Project<M>(string sourceId)
    {
        await using SqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        var command = "select * from es.eventsources where longsourceid=@sourceId";
        await using SqlCommand sqlCommand = new(command, sqlConnection);
        sqlCommand.Parameters.AddWithValue("@sourceId", sourceId);
        await using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();

        List<SourcedEvent> events = [];
        while(await reader.ReadAsync())
        {
            var typeName = reader.GetString(EventSourceSchema.Type);
            var type = ResolveEventType(typeName);
            var json = reader.GetString(EventSourceSchema.Data);
            var sourcedEvent = JsonSerializer.Deserialize(json, type) as SourcedEvent??
                throw new Exception("deserialization failure.");
            events.Add(sourcedEvent);
        }

        var projection = sp.GetRequiredService<IProjectionEngine>();
        var model = projection.Project<M>(events);
        return model;
    }
}